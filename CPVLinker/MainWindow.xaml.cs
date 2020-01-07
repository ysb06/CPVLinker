using Microsoft.Win32;
using System;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CPVLinker.Clova;
using CPVLinker.Tools;
using System.Windows.Media;
using System.Windows.Threading;

namespace CPVLinker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string Default_Client_ID = "1e682uafkp";
        private static string Default_Client_Secret = "8jbC1zXYimOqHXWDCcGb1KscBKERda5AqzEGJW6h";

        private CpvLinkerCore cpvLinker = new CpvLinkerCore(Default_Client_ID, Default_Client_Secret);
        private string tempCsvPath = string.Empty;
        private bool isRequesting = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SettingManager.Load();
            cpvLinker.TargetDirectory = SettingManager.Path;
            cpvLinker.ClientID = SettingManager.ClientID;
            cpvLinker.ClientSecret = SettingManager.ClientSecret;

            PathSet_TextBox.Text = cpvLinker.TargetDirectory;
            ClientID_TextBox.Text = cpvLinker.ClientID;
            ClientSecret_TextBox.Text = cpvLinker.ClientSecret;

            cpvLinker.RequestFinished += CpvLinker_RequestFinished;
        }

        private void Content_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Textcount_Label.Content = Content_TextBox.Text.Length;
        }

        private void PathSet_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(PathSet_TextBox.Text))
            {
                cpvLinker.TargetDirectory = PathSet_TextBox.Text;
                MessageBox.Show("Path set complete", "Path Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if(
                    MessageBox.Show(
                        "There is no exist directory. Do you want to create it?", 
                        "No Directory", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question
                        )
                    == MessageBoxResult.Yes)
                {
                    try
                    {
                        string path = Directory.CreateDirectory(PathSet_TextBox.Text).FullName;
                        cpvLinker.TargetDirectory = path;
                        MessageBox.Show(path, "Path Set Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    }
                    catch(ArgumentException)
                    {
                        MessageBox.Show("Path Set Failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("No Changes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            PathSet_TextBox.Text = cpvLinker.TargetDirectory;
            Input_TextBox_LostFocus(sender, e);
        }

        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            if (isRequesting)
            {
                MessageBox.Show("요청 중입니다. 잠시 후 다시 시도해 주세요.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                StatusLight.Fill = Brushes.Red;
                string text = Content_TextBox.Text;
                isRequesting = true;

                Thread thread = new Thread(() => cpvLinker.RequestVoice(text));
                thread.Start();
            }
        }

        private void CpvLinker_RequestFinished(string requestedText, RequestResult requestResult)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate { StatusLight.Fill = Brushes.LightGreen; }));
            isRequesting = false;

            if (requestResult.IsSuccessed)
            {
                MessageBox.Show(requestResult.ResultPath, requestResult.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadData_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "UTF8 CSV 파일 (*.csv)|*.csv",
                CheckFileExists = true
            };
            if (tempCsvPath.Equals(string.Empty) == false)
            {
                dialog.InitialDirectory = tempCsvPath;
            }
            bool? result = dialog.ShowDialog();

            if (result == null) MessageBox.Show("Error");
            else if(result.Value)
            {
                tempCsvPath = dialog.FileName;
                cpvLinker.RequstVoiceFromCSV(dialog.FileName);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SettingManager.Path = cpvLinker.TargetDirectory;
            SettingManager.ClientID = cpvLinker.ClientID;
            SettingManager.ClientSecret = cpvLinker.ClientSecret;
            SettingManager.Save();
            cpvLinker.Dispose();
        }

        private void quitMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Activation_Button_Click(object sender, RoutedEventArgs e)
        {
            cpvLinker.ClientID = ClientID_TextBox.Text;
            cpvLinker.ClientSecret = ClientSecret_TextBox.Text;
            MessageBox.Show("Activation key changed");

            Input_TextBox_LostFocus(sender, e);
        }

        private void Input_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(ClientID_TextBox.Text.Equals(cpvLinker.ClientID) == false)
            {
                ClientID_TextBox.Background = Brushes.Red;
                ClientID_TextBox.Foreground = Brushes.White;
            }
            else
            {
                ClientID_TextBox.ClearValue(BackgroundProperty);
                ClientID_TextBox.ClearValue(ForegroundProperty);
            }

            if (ClientSecret_TextBox.Text.Equals(cpvLinker.ClientSecret) == false)
            {
                ClientSecret_TextBox.Background = Brushes.Red;
                ClientSecret_TextBox.Foreground = Brushes.White;
            }
            else
            {
                ClientSecret_TextBox.ClearValue(BackgroundProperty);
                ClientSecret_TextBox.ClearValue(ForegroundProperty);
            }

            if (PathSet_TextBox.Text.Equals(cpvLinker.TargetDirectory) == false)
            {
                PathSet_TextBox.Background = Brushes.Red;
                PathSet_TextBox.Foreground = Brushes.White;
            }
            else
            {
                PathSet_TextBox.ClearValue(BackgroundProperty);
                PathSet_TextBox.ClearValue(ForegroundProperty);
            }
        }

        private void helpMenu_Click(object sender, RoutedEventArgs e)
        {
            new HelpWindow().Show();
        }
    }
}
