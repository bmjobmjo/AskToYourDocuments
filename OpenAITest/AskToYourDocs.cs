using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OpenAITest
{
    public partial class AskToYourDocs : Form
    {
        public AskToYourDocs()
        {
            InitializeComponent();
            textBoxKey.Text = OpenAIHelper.GetLicenseKey();
        }


        private string GetFolderPathFromUser()
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select a folder containing files for Adding";

                // Show the FolderBrowserDialog to the user.
                DialogResult result = folderBrowserDialog.ShowDialog();

                // Check if the user clicked the "OK" button in the dialog.
                if (result == DialogResult.OK)
                {
                    // Get the selected folder path and return it.
                    return folderBrowserDialog.SelectedPath;
                }
                else
                {
                    // Handle the case where the user canceled the dialog or an error occurred.
                    // You can return null or an empty string, or throw an exception, depending on your application's behavior.
                    return null;
                }
            }
        }

        private string GetFilePathFromUser()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "Supported Files|*.rtf;*.doc;*.docx;*.pdf|All Files|*.*";

                // Show the OpenFileDialog to the user.
                DialogResult result = openFileDialog.ShowDialog();

                // Check if the user clicked the "OK" button in the dialog.
                if (result == DialogResult.OK)
                {
                    // Get the selected file path and return it.
                    return openFileDialog.FileName;
                }
                else
                {
                    // Handle the case where the user canceled the dialog or an error occurred.
                    // You can return null or an empty string, or throw an exception, depending on your application's behavior.
                    return null;
                }
            }
        }

        private async void buttonAddDoc_Click_1(object sender, EventArgs e)
        {
            if (textBoxFilePath.Text.Length < 5)
            {
                MessageBox.Show("Select a file");
                return;
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string embeddingsFolderPath = Path.Combine(documentsPath, "AskYourDocs", "Embeddings");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(embeddingsFolderPath))
            {
                Directory.CreateDirectory(embeddingsFolderPath);
            }

            try
            {
                bool success = await OpenAIHelper.AddDocumentToEmbeddings(textBoxFilePath.Text, embeddingsFolderPath);
                if (success)
                {
                    MessageBox.Show("Added Document to knowledgebase");
                }
                else
                {
                    MessageBox.Show("Failed to add document to knowledgebase");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding document to knowledgebase: " + ex.Message);
            }
        }

        private void buttonSelectFile_Click_1(object sender, EventArgs e)
        {
            textBoxFilePath.Text = GetFilePathFromUser();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBoxKey.Text.Length > 20)
            {
                OpenAIHelper.SetLicenseKey(textBoxKey.Text);
            }
            else
            {
                MessageBox.Show("Enter a valid key");
            }
        }

        private async void  button1_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string embeddingsFolderPath = Path.Combine(documentsPath, "AskYourDocs", "Embeddings");
                String question = textBoxQuestion.Text;

                int progress = 0;
                System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer();
                progressTimer.Interval = 100; // Adjust the interval as needed
                progressTimer.Tick += (s, ev) =>
                {
                    // Update the progress bar value based on the progress of awaitable operations
                    progressBar1.Value = ++progress;
                    if (progress >= 100) progress = 0;
                };
                progressTimer.Start();

                String embeddings = await OpenAIHelper.SearchEmbeddings(embeddingsFolderPath,question);
                if (!string.IsNullOrEmpty(embeddings))
                {

                    String answer = await OpenAIHelper.AskGPT(question, embeddings, null);  // Assuming you don't have a conversation history
                    textBoxAnswer.Text = answer;
                }
                else
                {
                    MessageBox.Show("No document found that matches the context of the given question.");
                }
                // Stop the progress timer
                progressBar1.Value = 0;
                progressTimer.Stop();

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
                // Stop the progress timer
               
            }
        }
    }
}
