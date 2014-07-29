#region Using

using System.ComponentModel.Composition;
using LightPaper.Infrastructure.Contracts;

#endregion

namespace LightPaper.Plugins.AutoSync
{
    [Export(typeof (IPreviewQuickOptionControl))]
    [Export(typeof (ScrollSyncQuickOption))]
    public partial class ScrollSyncQuickOption : IPreviewQuickOptionControl, IPartImportsSatisfiedNotification
    {
        #region Fields 

#pragma warning disable 649
        [Import] private ScrollSynchronizer _viewModel;
#pragma warning restore 649

        #endregion

        #region Constructors 

        public ScrollSyncQuickOption()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods 

        public void OnImportsSatisfied()
        {
            DataContext = _viewModel;
        }

        #endregion
    }
}