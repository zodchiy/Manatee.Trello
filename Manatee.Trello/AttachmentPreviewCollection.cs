﻿using Manatee.Trello.Internal.Synchronization;

namespace Manatee.Trello
{
	public class ReadOnlyAttachmentPreviewCollection : ReadOnlyCollection<AttachmentPreview>
	{
		private readonly AttachmentContext _context;

		internal ReadOnlyAttachmentPreviewCollection(AttachmentContext context)
			: base(context.Data.Id)
		{
			_context = context;
		}

		protected override sealed void Update()
		{
			_context.Synchronize();
			if (_context.Data.Previews == null) return;
			Items.Clear();
			foreach (var jsonPreview in _context.Data.Previews)
			{
				var preview = TrelloConfiguration.Cache.Find<AttachmentPreview>(ap => ap.Id == jsonPreview.Id) ?? new AttachmentPreview(jsonPreview);
				Items.Add(preview);
			}
		}
	}
}