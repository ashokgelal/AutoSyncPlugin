#region Using

using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using LightPaper.Infrastructure;
using LightPaper.Infrastructure.Contracts;
using LightPaper.Infrastructure.Helpers;
using LightPaper.Plugins.AutoSync.Properties;
using PuppyFramework.Interfaces;

#endregion

namespace LightPaper.Plugins.AutoSync
{
    [Export(typeof (ILightPaperPlugin))]
    [Export(typeof (ScrollSynchronizer))]
    public class ScrollSynchronizer : ViewModelBase, ILightPaperPlugin
    {
        #region Fields 

        private readonly IWorkingContentEditorsProvider _contentEditorsProvider;
        private readonly IPreview _preview;
        private bool _enableAutoSync;
        private WeakReference<ScrollViewer> _weakScrollViewer;

        #endregion

        #region Properties 

        public bool EnableAutoSync
        {
            get { return _enableAutoSync; }
            set
            {
                if (!SetProperty(ref _enableAutoSync, value)) return;
                Settings.Default._enableAutoSync = value;
                if (value)
                {
                    HookCurrentScrollViewer();
                }
                else
                {
                    UnHookCurrentScrollViewer();
                }
            }
        }

        #endregion

        #region Constructors 

        [ImportingConstructor]
        public ScrollSynchronizer(ILogger logger, IWorkingContentEditorsProvider contentEditorsProvider, IPreview preview) : base(logger)
        {
            _contentEditorsProvider = contentEditorsProvider;
            _preview = preview;
            Initialize();
            HookEvents();
        }

        private void Initialize()
        {
            EnableAutoSync = Settings.Default._enableAutoSync;
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
            var scrollViewer = UnHookCurrentScrollViewer();
            var editor = _contentEditorsProvider.CurrentContentEditor<TextEditor>();
            if (editor != null)
            {
                scrollViewer = editor.FindVisualChild<ScrollViewer>();
            }
            if (scrollViewer == null) return;
            if (EnableAutoSync)
            {
                scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
            }
            _weakScrollViewer = new WeakReference<ScrollViewer>(scrollViewer);
        }

        private void HookCurrentScrollViewer()
        {
            ScrollViewer scrollViewer;
            if (_weakScrollViewer != null && _weakScrollViewer.TryGetTarget(out scrollViewer))
            {
                scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
            }
        }

        private ScrollViewer UnHookCurrentScrollViewer()
        {
            ScrollViewer scrollViewer = null;
            if (_weakScrollViewer != null && _weakScrollViewer.TryGetTarget(out scrollViewer))
            {
                scrollViewer.ScrollChanged -= ScrollViewerOnScrollChanged;
            }
            return scrollViewer;
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