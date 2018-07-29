using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Extensions;
using OrchardCore.Recipes.Services;
using OrchardCore.Recipes.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.Recipes.Controllers
{
    [Admin]
    public class AdminController : Controller
    {
        private readonly IExtensionManager _extensionManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEnumerable<IRecipeHarvester> _recipeHarvesters;
        private readonly INotifier _notifier;
        private readonly IRecipeExecutor _recipeExecutor;
        private readonly ISiteService _siteService;

        public AdminController(
            ISiteService siteService,
            IAdminThemeService adminThemeService,
            IExtensionManager extensionManager,
            IHtmlLocalizer<AdminController> localizer,
            IAuthorizationService authorizationService,
            IEnumerable<IRecipeHarvester> recipeHarvesters,
            IRecipeExecutor recipeExecutor,
            INotifier notifier)
        {
            _siteService = siteService;
            _recipeExecutor = recipeExecutor;
            _extensionManager = extensionManager;
            _authorizationService = authorizationService;
            _recipeHarvesters = recipeHarvesters;
            _notifier = notifier;

            T = localizer;
        }

        public IHtmlLocalizer T { get; }

        public async Task<ActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.RecipeExecutor))
            {
                return Unauthorized();
            }

            var recipeCollections = await Task.WhenAll(_recipeHarvesters.Select(x => x.HarvestRecipesAsync()));
            var recipes = recipeCollections.SelectMany(x => x);

            recipes = recipes.Where(c => !c.Tags.Contains("hidden", StringComparer.InvariantCultureIgnoreCase));

            var features = _extensionManager.GetFeatures();

            var model = recipes.Select(recipe => new RecipeViewModel
            {
                Name = recipe.Name,
                FileName = recipe.RecipeFileInfo.Name,
                BasePath = recipe.BasePath,
                Tags = recipe.Tags,
                IsSetupRecipe = recipe.IsSetupRecipe,
                Feature = features.FirstOrDefault(f=>recipe.BasePath.Contains(f.Extension.SubPath))?.Name,
                Description = recipe.Description
            }).ToArray();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Execute(string basePath, string fileName)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.RecipeExecutor))
            {
                return Unauthorized();
            }

            var recipeCollections = await Task.WhenAll(_recipeHarvesters.Select(x => x.HarvestRecipesAsync()));
            var recipes = recipeCollections.SelectMany(x => x);

            var recipe = recipes.FirstOrDefault(c => c.RecipeFileInfo.Name == fileName && c.BasePath == basePath);

            if (recipe == null)
            {
                _notifier.Error(T["Recipe was not found"]);
                return RedirectToAction("Index");
            }

            var site = await _siteService.GetSiteSettingsAsync();
            var executionId = Guid.NewGuid().ToString("n");

            await _recipeExecutor.ExecuteAsync(executionId, recipe, new
            {
                site.SiteName,
                AdminUsername = User.Identity.Name,
            });

            _notifier.Success(T["The recipe '{0}' has been run successfully", recipe.Name]);
            return RedirectToAction("Index");
        }
    }
}
