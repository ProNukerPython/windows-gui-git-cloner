using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Web;
using LibGit2Sharp;

namespace WindowsGuiGitCloner
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private Button cloneButton;
        private Button signInButton;
        private Label statusLabel;
        private TextBox pathTextBox;
        private Button browseButton;
        private PictureBox logoPictureBox;
        private string accessToken;

        public MainForm()
        {
            this.Text = "Git Cloner";
            this.Width = 600;
            this.Height = 600;

            logoPictureBox = new PictureBox()
            {
                Left = 50,
                Top = 50,
                Width = 500,
                Height = 200,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            try
            {
                logoPictureBox.Image = Image.FromFile("LogoGitClonner.jpg");
                this.Icon = Icon.FromHandle(((Bitmap)logoPictureBox.Image).GetHicon());
            }
            catch (System.IO.FileNotFoundException)
            {
                MessageBox.Show("LogoGitClonner.jpg not found. Please ensure the file is in the src directory and set to copy to the output directory.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            pathTextBox = new TextBox() { Left = 10, Top = 270, Width = 480 };
            browseButton = new Button() { Text = "Browse", Left = 500, Top = 270, Width = 70 };
            cloneButton = new Button() { Text = "Clone", Left = 10, Top = 300, Width = 560 };
            signInButton = new Button() { Text = "Sign In", Left = 10, Top = 330, Width = 560, Enabled = false };
            statusLabel = new Label() { Left = 10, Top = 360, Width = 560 };

            browseButton.Click += BrowseButton_Click;
            cloneButton.Click += CloneButton_Click;
            signInButton.Click += SignInButton_Click;

            this.Controls.Add(logoPictureBox);
            this.Controls.Add(pathTextBox);
            this.Controls.Add(browseButton);
            this.Controls.Add(cloneButton);
            this.Controls.Add(signInButton);
            this.Controls.Add(statusLabel);

            HandleOAuthCallback();
        }

        private async void HandleOAuthCallback()
        {
            if (HttpListener.IsSupported)
            {
                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add("http://localhost:5000/callback/");
                    listener.Start();

                    var context = await listener.GetContextAsync();
                    var code = context.Request.QueryString["code"];

                    if (!string.IsNullOrEmpty(code))
                    {
                        await ExchangeCodeForToken(code);

                        // Send a response to close the browser window
                        var response = context.Response;
                        string responseString = "<html><body><script type='text/javascript'>window.close();</script></body></html>";
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        var output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();
                    }

                    listener.Stop();
                }
            }
        }

        private async Task ExchangeCodeForToken(string code)
        {
            string clientId = "Ov23li69LxhJlNL6KNNg"; // Replace with your actual client ID
            string clientSecret = "79b4941d25146d069846a7f354f8595161ba6e3d"; // Replace with your actual client secret
            string redirectUri = "http://localhost:5000/callback"; // Ensure this matches the callback URL registered with GitHub

            try
            {
                using (var client = new HttpClient())
                {
                    var tokenResponse = await client.PostAsync("https://github.com/login/oauth/access_token", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", redirectUri)
                    }));

                    var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                    accessToken = HttpUtility.ParseQueryString(responseContent)["access_token"];

                    this.Invoke((MethodInvoker)delegate
                    {
                        statusLabel.Text = "Sign In successful, click Clone.";
                        signInButton.Enabled = false;
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = $"Error during authentication: {ex.Message}";
                });
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    pathTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private async void CloneButton_Click(object sender, EventArgs e)
        {
            string repoUrl = "https://github.com/ProNukerPython/MarcTools";
            string localPath = pathTextBox.Text;

            if (string.IsNullOrEmpty(accessToken))
            {
                statusLabel.Text = "Repository is private. Please sign in.";
                signInButton.Enabled = true;
                return;
            }

            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

            try
            {
                var cloneOptions = new CloneOptions
                {
                    CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = accessToken, Password = "x-oauth-basic" },
                    IsBare = false,
                    Checkout = true,
                    RecurseSubmodules = true,
                    OnTransferProgress = progress =>
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            statusLabel.Text = $"Transferring: {progress.ReceivedObjects}/{progress.TotalObjects}";
                        });
                        return true;
                    },
                    OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            statusLabel.Text = $"Checking out: {completedSteps}/{totalSteps}";
                        });
                    }
                };

                await Task.Run(() => Repository.Clone(repoUrl, tempPath, cloneOptions));
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = "Clone successful!";
                });

                foreach (string dirPath in System.IO.Directory.GetDirectories(tempPath, "*", System.IO.SearchOption.AllDirectories))
                {
                    System.IO.Directory.CreateDirectory(dirPath.Replace(tempPath, localPath));
                }

                foreach (string newPath in System.IO.Directory.GetFiles(tempPath, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    try
                    {
                        System.IO.File.Copy(newPath, newPath.Replace(tempPath, localPath), true);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        System.IO.File.SetAttributes(newPath, System.IO.FileAttributes.Normal);
                        System.IO.File.Copy(newPath, newPath.Replace(tempPath, localPath), true);
                    }
                }

                string nukeFolderPath = localPath;
                string menuFilePath = System.IO.Path.Combine(nukeFolderPath, "menu.py");

                if (!System.IO.Directory.Exists(nukeFolderPath))
                {
                    System.IO.Directory.CreateDirectory(nukeFolderPath);
                }

                if (!System.IO.File.Exists(menuFilePath))
                {
                    System.IO.File.WriteAllText(menuFilePath, string.Empty);
                }

                DialogResult result = MessageBox.Show("Do you want to add lines to menu.py automatically?", "Add Lines", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string[] linesToAdd = {
                        "import importlib",
                        "import MarcTools",
                        "",
                        "importlib.reload(MarcTools)"
                    };
                    System.IO.File.AppendAllLines(menuFilePath, linesToAdd);
                    MessageBox.Show("Lines added to menu.py successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = $"Git Error: {ex.Message}";
                });
            }
            catch (System.IO.FileNotFoundException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = $"File Error: {ex.Message}";
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = $"Access Error: {ex.Message}";
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = $"Error: {ex.Message}";
                });
            }
            finally
            {
                try
                {
                    if (System.IO.Directory.Exists(tempPath))
                    {
                        foreach (string file in System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories))
                        {
                            System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
                        }
                        System.IO.Directory.Delete(tempPath, true);
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        statusLabel.Text = $"Error deleting temp directory: {ex.Message}";
                    });
                }

                Application.Exit();
            }
        }

        private void SignInButton_Click(object sender, EventArgs e)
        {
            OpenBrowserForOAuth();
        }

        private void OpenBrowserForOAuth()
        {
            string clientId = "Ov23li69LxhJlNL6KNNg"; // Replace with your actual client ID
            string redirectUri = "http://localhost:5000/callback"; // Ensure this matches the callback URL registered with GitHub
            string authorizationEndpoint = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&scope=repo&response_type=code";

            Process.Start(new ProcessStartInfo
            {
                FileName = authorizationEndpoint,
                UseShellExecute = true
            });
        }
    }
}
