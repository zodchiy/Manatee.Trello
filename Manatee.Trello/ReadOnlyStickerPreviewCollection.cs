﻿using System.Threading;
using System.Threading.Tasks;
using Manatee.Trello.Internal.Caching;
using Manatee.Trello.Internal.Synchronization;

namespace Manatee.Trello
{
	/// <summary>
	/// A read-only collection of image previews for attachments.
	/// </summary>
	public class ReadOnlyStickerPreviewCollection : ReadOnlyCollection<IImagePreview>
	{
		private readonly StickerContext _context;

		internal ReadOnlyStickerPreviewCollection(StickerContext context, TrelloAuthorization auth)
			: base(() => context.Data.Id, auth)
		{
			_context = context;
		}

		internal sealed override async Task PerformRefresh(bool force, CancellationToken ct)
		{
			await _context.Synchronize(force, ct);
			if (_context.Data.Previews == null) return;
			Items.Clear();
			foreach (var jsonPreview in _context.Data.Previews)
			{
				var preview = jsonPreview.GetFromCache<ImagePreview>(Auth);
				Items.Add(preview);
			}
		}
	}
}