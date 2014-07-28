#region Using

using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using LightPaper.Infrastructure.Contracts;
using LightPaper.Infrastructure.Helpers;

#endregion

namespace LightPaper.Plugins.AutoSync
{
    [Export(typeof (ILightPaperPlugin))]
    [Export(typeof (ScrollSynchronizer))]
    public class ScrollSynchronizer : ILightPaperPlugin
    {
        #region Fields 

        private readonly IWorkingContentEditorsProvider _contentEditorsProvider;
        private readonly IPreview _preview;
        private WeakReference<TextEditor> _currentEditor;
        private WeakReference<ScrollViewer> _weakScrollViewer;

        #endregion

        #region Constructors 

        [ImportingConstructor]
        public ScrollSynchronizer(IWorkingContentEditorsProvider contentEditorsProvider, IPreview preview)
        {
            _contentEditorsProvider = contentEditorsProvider;
            _preview = preview;
            HookEvents();
        }

        #endregion

        #region Methods

        private const double TOLERANCE = 0.00001;

        private void HookEvents()
        {
            _contentEditorsProvider.CollectionChangedEvent += ContentEditorsProvider_CollectionChangedEventHandler;
        }

        private void ContentEditorsProvider_CollectionChangedEventHandler(object sender, SelectionChangedEventArgs e)
        {
            ScrollViewer scrollViewer;
            if (_currentEditor != null)
            {
                if (_weakScrollViewer.TryGetTarget(out scrollViewer))
                {
                    scrollViewer.ScrollChanged -= ScrollViewerOnScrollChanged;
                }
            }
            var editor = _contentEditorsProvider.CurrentContentEditor<TextEditor>();
            scrollViewer = editor.FindVisualChild<ScrollViewer>();
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
                _weakScrollViewer = new WeakReference<ScrollViewer>(scrollViewer);
            }
            _currentEditor = new WeakReference<TextEditor>(editor);
        }

        private async void ScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            if (Math.Abs(args.ViewportHeightChange) < TOLERANCE && Math.Abs(args.ViewportWidthChange) < TOLERANCE && Math.Abs(args.VerticalChange) < TOLERANCE) return;
            var calculateScrollPosition = CalculateScrollPosition(sender as ScrollViewer);
            await _preview.ExecuteJavascriptAsync(string.Format("window.scroll(0, {0} * (document.body.scrollHeight));", calculateScrollPosition));
        }

        public double CalculateScrollPosition(ScrollViewer scrollViewer)
        {
            if (scrollViewer == null) return 0.0f;
            var diff = scrollViewer.ExtentHeight - scrollViewer.ViewportHeight;
            return Math.Abs(diff) < TOLERANCE ? 0.0f : scrollViewer.VerticalOffset/diff;
        }

        #endregion
    }
}