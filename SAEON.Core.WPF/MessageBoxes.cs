using System;
using System.Windows;

namespace SAEON.Core
{
    public static class MessageBoxes
    {
        public static bool ConfirmBox(string message, params object[] values)
        {
            return (MessageBox.Show(string.Format(message, values), "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
        } 

        public static void ErrorBox(string message, params object[] values)
        {
            MessageBox.Show(string.Format(message, values), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ExceptionBox(Exception ex, string message = "", params object[] values)
        {
            MessageBox.Show("Exception:" + Environment.NewLine + String.Format(message, values) + Environment.NewLine + ex.Message.Trim(),
                "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void InfoBox(string message, params object[] values)
        {
            MessageBox.Show(string.Format(message, values), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
