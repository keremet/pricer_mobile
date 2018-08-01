using System;
using System.IO;
using Xamarin.Forms;

namespace Pricer
{
    public class MainPage : MasterDetailPage
    {
        private ListView listView;
        private Label lblInfo;
        private string dir;

        private Entry _loginField;
        private Entry _pwdField;

        public MainPage()
        {
            listView = new ListView();
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "scanned");
            listView.ItemSelected += (sender, e) => {
                DisplayAlert("Содержимое QR кода", (e.SelectedItem as string), "Закрыть");
            };
            UpdateReceipts();

            Button btnScan = new Button
            {
                Text = "Сканировать",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Button)),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            btnScan.Clicked += OnBtnScanClicked;

            Button btnSnd = new Button
            {
                Text = "Отправить",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Button)),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            btnSnd.Clicked += OnBtnSndClicked;

            Button btnDel = new Button
            {
                Text = "Удалить",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Button)),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            btnDel.Clicked += OnBtnDelClicked;

            var stackLayoutButtons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Start,
                Children = {
                    btnScan, btnSnd, btnDel
                }
            };
            lblInfo = new Label() {
                Text = "",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            var stackLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Start,
                Children = {
                    stackLayoutButtons, lblInfo, listView
                }
            };

            ScrollView content = new ScrollView();
            content.Content = stackLayout;

            var leftTitle = new Label
            {
                Text = "Авторизация",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            var loginField = new Entry {Placeholder = "Логин", PlaceholderColor = Color.Aqua};
            var pwdField = new Entry { Placeholder = "Пароль", PlaceholderColor = Color.Aqua, IsPassword = true };
            var saveCredsBtn = new Button
            {
                Text = "Сохранить",
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Button)),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };

            saveCredsBtn.Clicked += saveCredsClicked;

            var leftLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                WidthRequest = 100,
                Children = {
                    leftTitle, loginField, pwdField, saveCredsBtn
                }
            };

            _loginField = loginField;
            _pwdField = pwdField;

            Master = new ContentPage
            {
                Title = "Ценовичок",
                Content = leftLayout
            };
            Detail = new ContentPage
            {
                Title = "Ценовичок",
                Content = content
            };

            if (Application.Current.Properties.ContainsKey("login"))
            {
                _loginField.Text = Application.Current.Properties["login"] as string;
                _pwdField.Text = Application.Current.Properties["password"] as string;
            }
        }

        private void saveCredsClicked(object sender, EventArgs e)
        {
            Application.Current.Properties["login"] = _loginField.Text;
            Application.Current.Properties["password"] = _pwdField.Text;
            DisplayAlert("Готово", "Логин и пароль успешно сохранены", "Закрыть");
        }

        private void UpdateReceipts()
        {
            string[] receipts = null;
            if (Directory.Exists(dir))
            {
                receipts = Directory.GetFiles(dir);

                for (int i = 0; i < receipts.Length; i++)
                {
                    receipts[i] = Path.GetFileName(receipts[i]);
                }
            }
           // string[] receipts = new string[] { "iPhone 7", "Samsung Galaxy S8", "Huawei P10", "LG G6" };
            listView.ItemsSource = receipts;
        }

        private void OnBtnScanClicked(object sender, System.EventArgs e)
        {
            PerformScanAsync();
        }

        private void OnBtnDelClicked(object sender, System.EventArgs e)
        {
            File.Delete(Path.Combine(dir, (listView.SelectedItem as string)));
            UpdateReceipts();
        }

        private async void OnBtnSndClicked(object sender, System.EventArgs e)
        {
            string login = _loginField.Text;
            if (String.IsNullOrWhiteSpace(login))
            {
                lblInfo.Text = "Введите логин";
                return;
            }

            string passwd = _pwdField.Text;
            if (String.IsNullOrWhiteSpace(passwd))
            {
                lblInfo.Text = "Введите пароль";
                return;
            }

            lblInfo.Text = "Ожидание ответа сервера";
            System.Net.WebRequest request = System.Net.WebRequest.Create(string.Format("{0}?login={1}&passwd={2}", "http://orv.org.ru/pricer/api/receipt/send.php", login, passwd));
            request.Method = "POST";
            var lst = listView.ItemsSource as string[];
            string sName = "";
            for(int i = 0; i < lst.Length; i++)
            {
                sName += lst[i] + ';';
            }
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(sName);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            try
            {
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
            }
            catch (Exception ex)
            {
                lblInfo.Text = "Ошибка отправки " + ex.Message;
            }

            try
            {
                System.Net.WebResponse response = await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                     using (StreamReader reader = new StreamReader(stream))
                     {
                        lblInfo.Text = reader.ReadToEnd();
                     }
                }
                response.Close();
            }
            catch (Exception ex)
            {
                lblInfo.Text = "Ошибка получения ответа " + ex.Message;
            }

        }

        async private void PerformScanAsync()
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();
            var result = await scanner.Scan();

            if (result == null)
                return;
            
            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fname = Path.Combine(dir, result.Text);
            if (File.Exists(fname))
            {
                lblInfo.Text = "Чек был добавлен ранее";
                return;
            }

            using (var streamWriter = new StreamWriter(fname, false))
            {
                streamWriter.WriteLine(result.Text);
            }
            lblInfo.Text = "Чек успешно добавлен";
            UpdateReceipts();
        }
    }
}

