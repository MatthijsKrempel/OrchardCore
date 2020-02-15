using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Lists.Indexes;
using System.Linq.Expressions;
using YesSql;

namespace OrchardCore.Lists.Helpers
{
    static class ListQueryHelpers
    {
        internal static async Task<int> QueryListItemsCountAsync(ISession session, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null)
        {
            return await session.Query<ContentItem>()
                    .With<ContainedPartIndex>(x => x.ListContentItemId == listContentItemId)
                    .With<ContentItemIndex>(itemPredicate ?? (x => x.Published))
                    .CountAsync();
        }

        internal static async Task<IEnumerable<ContentItem>> QueryListItemsAsync(ISession session, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null)
        {
            return await session.Query<ContentItem>()
                    .With<ContainedPartIndex>(x => x.ListContentItemId == listContentItemId)
                    .With<ContentItemIndex>(itemPredicate ?? (x => x.Published))
                    .ListAsync();
        }
    }
}
