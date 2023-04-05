using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace snowdata
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await FetchWeatherData();

        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                await FetchWeatherData();
            });

            await FetchWeatherData();
            await FetchNwsWeatherData(34.2439, -116.9114); // Use the coordinates of your location
        }

        private async void RefreshWeatherDataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await FetchWeatherData();
        }
        private double CalculateWetBulbTemperature(double temperature, double humidity)
        {
            double dewPoint = temperature - ((100 - humidity) / 5);
            double wetBulb = 0.56 * temperature + 0.393 * dewPoint + 3.94;
            return wetBulb;
        }
        private async Task FetchNwsWeatherData(double latitude, double longitude)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string nwsApiUrl = $"https://api.weather.gov/points/{latitude},{longitude}/forecast";
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("User-Agent", "SnowDataApp");

                    string nwsApiResponse = await client.GetStringAsync(nwsApiUrl);
                    JObject nwsApiData = JObject.Parse(nwsApiResponse);

                    string temperatureUrl = nwsApiData["properties"]["temperature"]["sourceUnit"].ToString();
                    string humidityUrl = nwsApiData["properties"]["relativeHumidity"]["sourceUnit"].ToString();



                    double nwsTemperature = Convert.ToDouble(await client.GetStringAsync(temperatureUrl));
                    double nwsHumidity = Convert.ToDouble(await client.GetStringAsync(humidityUrl));

                    double nwsWetBulbTemperature = CalculateWetBulbTemperature(nwsTemperature, nwsHumidity);

                    NwsTemperature.Text = $"Temperature: {nwsTemperature}°F";
                    NwsHumidity.Text = $"Humidity: {nwsHumidity}%";
                    NwsWetBulbTemperature.Text = $"Wet Bulb Temperature: {nwsWetBulbTemperature:F1}°F";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching NWS data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task FetchWeatherData()
        {
            // Replace with your API keys
            string openWeatherApiKey = "41007d54b06b17d82ba0b16d1541c2f5";
            string weatherApiApiKey = "c3bd11a74bf642e9a5e225108230404";

            // Set the location
            string location = "Big%20Bear%20Lake,CA";

            JObject openWeatherData = null;
            double openWeatherTemperature = 0;
            JObject weatherApiData = null;
            double weatherApiTemperature = 0;

            try
            {
                // Fetch data from OpenWeatherMap
                using (HttpClient client = new HttpClient())
                {
                    string openWeatherUrl = $"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={openWeatherApiKey}&units=imperial";
                    string openWeatherResponse = await client.GetStringAsync(openWeatherUrl);
                    openWeatherData = JObject.Parse(openWeatherResponse);

                    openWeatherTemperature = openWeatherData["main"]["temp"].ToObject<double>();
                    int openWeatherCloudCoverage = openWeatherData["clouds"]["all"].ToObject<int>();

                    OpenWeatherTemperature.Text = $"Temperature: {openWeatherTemperature}°F";
                    UpdateSnowMakingIndicator(openWeatherTemperature);
                    OpenWeatherCloudCoverage.Text = $"Cloud Coverage: {openWeatherCloudCoverage}%";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching OpenWeatherMap data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                // Fetch data from WeatherAPI
                using (HttpClient client = new HttpClient())
                {
                    string weatherApiUrl = $"https://api.weatherapi.com/v1/current.json?key={weatherApiApiKey}&q={location}&aqi=no";
                    string weatherApiResponse = await client.GetStringAsync(weatherApiUrl);
                    weatherApiData = JObject.Parse(weatherApiResponse);

                    weatherApiTemperature = weatherApiData["current"]["temp_f"].ToObject<double>();
                    int weatherApiCloudCoverage = weatherApiData["current"]["cloud"].ToObject<int>();

                    WeatherApiTemperature.Text = $"Temperature: {weatherApiTemperature}°F";
                    UpdateSnowMakingIndicator(weatherApiTemperature);
                    WeatherApiCloudCoverage.Text = $"Cloud Coverage: {weatherApiCloudCoverage}%";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching WeatherAPI data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            // Display data for OpenWeatherMap
            if (openWeatherData != null)
            {
                double openWeatherHumidity = openWeatherData["main"]["humidity"].ToObject<double>();
                double openWeatherWetBulbTemperature = CalculateWetBulbTemperature(openWeatherTemperature, openWeatherHumidity);
                OpenWeatherHumidity.Text = $"Humidity: {openWeatherHumidity}%";
                OpenWeatherWetBulbTemperature.Text = $"Wet Bulb Temperature: {openWeatherWetBulbTemperature:F1}°F";
            }

            // Display data for WeatherAPI
            if (weatherApiData != null)
            {
                double weatherApiHumidity = weatherApiData["current"]["humidity"].ToObject<double>();
                double weatherApiWetBulbTemperature = CalculateWetBulbTemperature(weatherApiTemperature, weatherApiHumidity);
                WeatherApiHumidity.Text = $"Humidity: {weatherApiHumidity}%";
                WeatherApiWetBulbTemperature.Text = $"Wet Bulb Temperature: {weatherApiWetBulbTemperature:F1}°F";
            }
        }
        private void UpdateSnowMakingIndicator(double temperature)
        {
            const double snowMakingThreshold = 32.0; // Temperature threshold for snowmaking in Fahrenheit (change this value if needed)

            if (temperature <= snowMakingThreshold)
            {
                SnowMakingIndicator.Text = "Yes"; // Green light (snowmaking is possible)
            }
            else
            {
                SnowMakingIndicator.Text = "No"; // Red light (snowmaking is not possible)
            }
        }



    }
}


