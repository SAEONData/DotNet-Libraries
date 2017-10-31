using System.ComponentModel;
using System.Windows;

namespace SAEON.Core
{
    public static class DesignerHelper
    {
        public static bool IsInDesignMode { get { return DesignerProperties.GetIsInDesignMode(new DependencyObject()) || (LicenseManager.UsageMode == LicenseUsageMode.Designtime); } }
    }
}
