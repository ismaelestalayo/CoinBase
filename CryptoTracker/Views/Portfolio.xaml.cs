﻿using CryptoTracker.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace CryptoTracker {

    public class SuggestionCoinList {
        public string Icon { get; set; }
        public string Name { get; set; }
    }

    public partial class Portfolio : Page {

        static ObservableCollection<PurchaseClass> dataList { get; set; }
        private double curr = 0;

        public Portfolio() {
            this.InitializeComponent();

            List<string> coinsArray = new List<string>();
            foreach (JSONcoins coin in App.coinList)
                coinsArray.Add(coin.Name);
            CryptoComboBox.ItemsSource = coinsArray;

            dataList = ReadPortfolio().Result;
            dataGridd.ItemsSource = dataList;


            UpdatePortfolio();
        }

        // ###############################################################################################
        //  Add new purchase
        private void AddButton_Click(object sender, RoutedEventArgs e) {
            
            try {
                string crypto = CryptoComboBox.SelectedItem.ToString();
                curr = Math.Round(App.GetCurrentPrice(crypto, "defaultMarket"), 4);

                // Calculate earnings/losings
                double priceBought = (1 / double.Parse(cryptoQtyTextBox.Text)) * double.Parse(investedQtyTextBox.Text);
                priceBought = Math.Round(priceBought, 4);
                double earningz = Math.Round((curr - priceBought) * double.Parse(cryptoQtyTextBox.Text), 5);

                // Get logo for the coin
                string logoURL = "Assets/Icons/icon" + crypto + ".png";
                if (!File.Exists(logoURL))
                    logoURL = "https://chasing-coins.com/coin/logo/" + crypto;
                else
                    logoURL = "/" + logoURL;
                

                dataList.Add(new PurchaseClass {
                    Crypto      = crypto,
                    CryptoLogo  = logoURL,
                    CryptoQty   = Math.Round(double.Parse(cryptoQtyTextBox.Text), 5),
                    Date        = DateTime.Today,
                    Delta       = Math.Round( (curr / priceBought), 2) * 100, // percentage
                    InvestedQty = double.Parse(investedQtyTextBox.Text),
                    BoughtAt    = Math.Round(priceBought, 4),
                    arrow       = earningz < 0 ? "▼" : "▲",
                    c           = App.coinSymbol,
                    Current     = curr,
                    Profit      = Math.Round(Math.Abs(earningz), 2).ToString(),
                    ProfitFG    = (earningz < 0) ? (SolidColorBrush)App.Current.Resources["pastelRed"] : (SolidColorBrush)App.Current.Resources["pastelGreen"],
                    Worth       = Math.Round( curr * double.Parse(cryptoQtyTextBox.Text), 2)
                });

                // Clear user input
                cryptoQtyTextBox.Text = String.Empty;
                investedQtyTextBox.Text = String.Empty;

                // Update and save
                UpdatePortfolio();
                SavePortfolio();

            } catch(Exception) {
                cryptoQtyTextBox.Text = String.Empty;
                investedQtyTextBox.Text = String.Empty;
            }
        }

        // ###############################################################################################
        //  For sync all
        internal void UpdatePortfolio() {

            for (int i = 0; i < ((Collection<PurchaseClass>)dataGridd.ItemsSource).Count; i++) {
                string crypto = dataList[i].Crypto;

                curr = Math.Round(App.GetCurrentPrice(crypto, "defaultMarket"), 4);

                dataList[i].Current = curr;
                double priceBought = (1 / dataList[i].CryptoQty) * dataList[i].InvestedQty;
                priceBought = Math.Round(priceBought, 4);

                double earningz = Math.Round((curr - priceBought) * dataList[i].CryptoQty, 4);
                dataList[i].arrow = earningz < 0 ? "▼" : "▲";
                dataList[i].Delta = Math.Round(curr / priceBought, 2) * 100; // percentage
                dataList[i].Profit = Math.Round(Math.Abs(earningz), 2).ToString();
                dataList[i].ProfitFG = (earningz < 0) ? (SolidColorBrush)App.Current.Resources["pastelRed"] : (SolidColorBrush)App.Current.Resources["pastelGreen"];
                dataList[i].Worth = Math.Round(curr * dataList[i].CryptoQty, 2);
            }
            UpdateProfits();
            SavePortfolio();
        }

        // ###############################################################################################
        private void UpdateProfits() {
            float total = 0;
            // empty chart
            portfolioChartGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < ((Collection<PurchaseClass>)dataGridd.ItemsSource).Count; i++) {
                total += float.Parse(dataList[i].Current.ToString()) * (float)dataList[i].CryptoQty;


                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(dataList[i].Worth, GridUnitType.Star);
                portfolioChartGrid.ColumnDefinitions.Add(col);

                var s = new StackPanel();
                s.BorderThickness = new Thickness(0);
                s.Margin = new Thickness(1, 0, 1, 0);
                s.BorderBrush = (SolidColorBrush) App.Current.Resources["TextBoxForegroundHeaderThemeBrush"];
                try { s.Background = (SolidColorBrush)App.Current.Resources[ dataList[i].Crypto + "_color"]; }
                catch { s.Background = (SolidColorBrush)App.Current.Resources["null_color"]; }

                portfolioChartGrid.Children.Add(s);
                Grid.SetColumn(s, i);
            }

        }

        private void RemovePortfolio_Click(object sender, RoutedEventArgs e) {
            if (dataGridd.SelectedIndex != -1) {
                dataList.RemoveAt(dataGridd.SelectedIndex);
                UpdatePortfolio();
                SavePortfolio();

                dataGridd.SelectedIndex = -1;
            }
        }

        // ###############################################################################################
        //  Save new purchase date
        private void purchaseDate_changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) {
            if (args.OldDate > new DateTime(2000, 01, 01, 00, 00, 00, 0) ) 
                SavePortfolio();
            
        }
        


        // ###############################################################################################
        //  Read/Write portfolio
        private static async void SavePortfolio() {
            try {
                StorageFile savedStuffFile =
                    await ApplicationData.Current.LocalFolder.CreateFileAsync("portfolio", CreationCollisionOption.ReplaceExisting);

                using (Stream writeStream =
                    await savedStuffFile.OpenStreamForWriteAsync().ConfigureAwait(false) ) {

                    DataContractSerializer stuffSerializer =
                        new DataContractSerializer(typeof(List<PurchaseClass>));

                    stuffSerializer.WriteObject(writeStream, dataList);
                    await writeStream.FlushAsync();
                    writeStream.Dispose();

                }
            } catch (Exception e) {
                var z = e.Message;
            }
        }
        private static async Task<ObservableCollection<PurchaseClass>> ReadPortfolio() {

            try {
                var readStream =
                    await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("portfolio").ConfigureAwait(false);

                DataContractSerializer stuffSerializer =
                    new DataContractSerializer(typeof(ObservableCollection<PurchaseClass>));

                var setResult = (ObservableCollection<PurchaseClass>)stuffSerializer.ReadObject(readStream);
                await readStream.FlushAsync();
                readStream.Dispose();

                return setResult;
            } catch (Exception ex) {
                var unusedWarning = ex.Message;
                return new ObservableCollection<PurchaseClass>();
            }
        }

        
    }
}
