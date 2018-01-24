using System.Windows;

namespace SciChart_50ChannelEEG
{
    public static class ViewWinUtils
    {
        public static Window CreateDefaultDialog()
        {
            return new Window()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Height = 300,
                Width = 300
            };
        }

        public static Window CreateDefaultDialog(object cnt)
        {
            return new Window()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Height = 300,
                Width = 300,
                Content = cnt
            };
        }

        public static void ShowDefaultDialog(object cnt)
        {
            var result = new Window()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Height = 300,
                Width = 300,
                Content = cnt
            };
            result.ShowDialog();
        }
    }
}
