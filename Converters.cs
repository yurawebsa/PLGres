using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CounterPlg {
    public class TabToButtonStyleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tab selectedTab && parameter is string tabName)
            {
                var selectedTabName = selectedTab.ToString();
                return selectedTabName.Equals(tabName, StringComparison.OrdinalIgnoreCase)
                    ? Application.Current.FindResource("menuButtonActive")
                    : Application.Current.FindResource("menuButton");
            }
            return Application.Current.FindResource("menuButton");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tab selectedTab && parameter is string tabName)
            {
                var selectedTabName = selectedTab.ToString();
                return selectedTabName.Equals(tabName, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiTabToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tab selectedTab && parameter is string tabNames)
            {
                var selectedTabName = selectedTab.ToString();
                var allowedTabs = tabNames.Split(';');
                return allowedTabs.Any(tab => tab.Equals(selectedTabName, StringComparison.OrdinalIgnoreCase))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}