using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WeatherWPF
{
    public class ForecastItem
    {
        public string DateLabel { get; set; }
        public string IconUrl { get; set; }
        public string MaxTemp { get; set; }
        public string MinTemp { get; set; }
    }

    public partial class MainWindow : Window
    {
        private const string ApiKey = ApiConfig.ApiKey;
        private const string BaseUrl = "https://api.weatherapi.com/v1/forecast.json";
        private const string SettingsFile = "settings.json";

        private readonly HttpClient _httpClient = new HttpClient();
        private AppSettings _settings = new AppSettings();

        private bool _showingFavorites = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (!File.Exists(SettingsFile)) return;

            try
            {
                string json = File.ReadAllText(SettingsFile);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                if (!string.IsNullOrEmpty(_settings.LastCity))
                    cityTextBox.Text = _settings.LastCity;
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchWeather();
        }

        private async void CityTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SearchWeather();
        }

        private async Task SearchWeather()
        {
            string city = cityTextBox.Text.Trim();

            if (string.IsNullOrEmpty(city))
            {
                ShowError("Введите название города");
                return;
            }

            HideError();
            ShowLoading();
            historyPanel.Visibility = Visibility.Collapsed;
            searchButton.IsEnabled = false;

            try
            {
                string url = BaseUrl + "?key=" + ApiKey
                    + "&q=" + Uri.EscapeDataString(city)
                    + "&days=5&lang=ru";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = ParseApiError(json, (int)response.StatusCode);
                    ShowError(errorMsg);
                    HideWeather();
                    return;
                }

                ApiResponse apiData = JsonSerializer.Deserialize<ApiResponse>(json);
                ShowWeather(apiData);

                string cityName = apiData.Location.Name;
                _settings.LastCity = cityName;
                AddToHistory(cityName);
            }
            catch (HttpRequestException)
            {
                ShowError("Нет подключения к интернету. Проверьте сеть и попробуйте снова.");
                HideWeather();
            }
            catch (Exception ex)
            {
                ShowError("Произошла ошибка: " + ex.Message);
                HideWeather();
            }
            finally
            {
                searchButton.IsEnabled = true;
            }
        }

        private string ParseApiError(string json, int statusCode)
        {
            try
            {
                ApiErrorResponse err = JsonSerializer.Deserialize<ApiErrorResponse>(json);
                int code = err.Error.Code;

                if (code == 1006) return "Город не найден. Проверьте название и попробуйте снова.";
                if (code == 2006 || code == 2007 || code == 2008) return "Ошибка ключа API.";
                if (code == 9999) return "Ошибка сервера.";

                return "Ошибка сервера (" + statusCode + "): " + err.Error.Message;
            }
            catch
            {
                return "Ошибка сервера: " + statusCode;
            }
        }

        private void ShowWeather(ApiResponse data)
        {
            hintTextBlock.Visibility = Visibility.Collapsed;
            loadingTextBlock.Visibility = Visibility.Collapsed;
            weatherPanel.Visibility = Visibility.Visible;

            cityNameTextBlock.Text = data.Location.Name + ", " + data.Location.Country;
            tempTextBlock.Text = data.Current.TempC.ToString("F0") + "°C";
            feelsLikeTextBlock.Text = "Ощущается как " + data.Current.FeelsLikeC.ToString("F0") + "°C";
            descTextBlock.Text = data.Current.Condition.Text;
            humidityTextBlock.Text = data.Current.Humidity + "%";
            windTextBlock.Text = data.Current.WindKph.ToString("F0") + " км/ч";
            pressureTextBlock.Text = data.Current.PressureMb.ToString("F0") + " гПа";

            try
            {
                string iconUrl = "https:" + data.Current.Condition.Icon;
                weatherIcon.Source = new BitmapImage(new Uri(iconUrl));
            }
            catch
            {
                weatherIcon.Source = null;
            }

            LoadForecast(data);
        }

        private void LoadForecast(ApiResponse data)
        {
            List<ForecastItem> items = new List<ForecastItem>();

            if (data.Forecast == null || data.Forecast.ForecastDay == null) return;

            string[] dayNames = { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

            foreach (ApiForecastDay day in data.Forecast.ForecastDay)
            {
                DateTime date = DateTime.Parse(day.Date);
                string label = date.Date == DateTime.Today
                    ? "Сегодня"
                    : dayNames[(int)date.DayOfWeek] + ", " + date.Day + " " + GetMonthShort(date.Month);

                ForecastItem item = new ForecastItem
                {
                    DateLabel = label,
                    IconUrl = "https:" + day.Day.Condition.Icon,
                    MaxTemp = day.Day.MaxTempC.ToString("F0") + "°",
                    MinTemp = day.Day.MinTempC.ToString("F0") + "°"
                };

                items.Add(item);
            }

            forecastList.ItemsSource = items;
        }

        private string GetMonthShort(int month)
        {
            string[] months = { "", "янв", "фев", "мар", "апр", "май", "июн",
                                 "июл", "авг", "сен", "окт", "ноя", "дек" };
            return months[month];
        }

        private void HideWeather()
        {
            weatherPanel.Visibility = Visibility.Collapsed;
            hintTextBlock.Visibility = Visibility.Visible;
            loadingTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowLoading()
        {
            hintTextBlock.Visibility = Visibility.Collapsed;
            loadingTextBlock.Visibility = Visibility.Visible;
            weatherPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            errorTextBlock.Text = "⚠ " + message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            errorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void AddToHistory(string city)
        {
            if (_settings.History.Contains(city))
                _settings.History.Remove(city);

            _settings.History.Insert(0, city);

            if (_settings.History.Count > 10)
                _settings.History.RemoveAt(_settings.History.Count - 1);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string city = cityNameTextBlock.Text;
            if (string.IsNullOrEmpty(city)) return;

            if (!_settings.Favorites.Contains(city))
            {
                _settings.Favorites.Add(city);
                SaveSettings();
                MessageBox.Show("Город добавлен в избранное!", "Избранное",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Этот город уже в избранном.", "Избранное",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            _showingFavorites = false;
            panelTitleTextBlock.Text = "История запросов";
            historyListBox.ItemsSource = null;
            historyListBox.ItemsSource = _settings.History;
            historyPanel.Visibility = Visibility.Visible;
        }

        private void ShowFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            _showingFavorites = true;
            panelTitleTextBlock.Text = "Избранные города";
            historyListBox.ItemsSource = null;
            historyListBox.ItemsSource = _settings.Favorites;
            historyPanel.Visibility = Visibility.Visible;
        }

        private async void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (historyListBox.SelectedItem == null) return;

            string selected = historyListBox.SelectedItem.ToString();
            string cityOnly = selected.Contains(",") ? selected.Split(',')[0].Trim() : selected;

            historyListBox.SelectedItem = null;
            historyPanel.Visibility = Visibility.Collapsed;
            cityTextBox.Text = cityOnly;
            await SearchWeather();
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (historyListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите элемент для удаления.", "Удаление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string item = historyListBox.SelectedItem.ToString();

            if (_showingFavorites)
            {
                _settings.Favorites.Remove(item);
                historyListBox.ItemsSource = null;
                historyListBox.ItemsSource = _settings.Favorites;
            }
            else
            {
                _settings.History.Remove(item);
                historyListBox.ItemsSource = null;
                historyListBox.ItemsSource = _settings.History;
            }

            SaveSettings();
        }

        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            historyPanel.Visibility = Visibility.Collapsed;
        }
    }
}