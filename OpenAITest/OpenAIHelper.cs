using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API.Embedding;
using OpenAI_API.Models;
using OpenAI_API;
using Xceed.Words.NET;
using NPOI.XWPF.UserModel;
using NPOI.SS.Formula.Functions;
using static NPOI.HSSF.Util.HSSFColor;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;
using OpenAI_API.Chat;
using System.Configuration;

namespace OpenAITest
{
    internal class OpenAIHelper
    {
        public static string GetLicenseKey()
        {
            // Retrieve the license key from application settings
            return Properties.Settings.Default.LicenseKey;
        }

        public static void SetLicenseKey(string licenseKey)
        {
            // Update the license key in application settings
            Properties.Settings.Default.LicenseKey = licenseKey;
            Properties.Settings.Default.Save();
          
        }
        public static async Task<bool> AddDocumentToEmbeddings(string filePath, string embeddingsFolder)
        {
            try
            {
                string originalFilenameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                OpenAIAPI api = new OpenAIAPI(OpenAIHelper.GetLicenseKey());
                // Step 1: Read the document's text using the ReadDocument function
                string documentText = ReadDocument(filePath);
                if (string.IsNullOrEmpty(documentText))
                {
                    Console.WriteLine("Document text could not be extracted.");
                    return false;
                }

                // Step 2: Count the tokens in the document using Tiktoken
                var encoding = Tiktoken.Encoding.ForModel("gpt-4");
                int totalTokens = encoding.CountTokens(documentText);

                // Step 3: Split the document into chunks if total tokens exceed 5000
                int maxTokensPerChunk = 5000;
                int splits = (int)Math.Ceiling((double)totalTokens / maxTokensPerChunk);
                int splitLength = (int)Math.Ceiling((double)documentText.Length / splits);

                // Step 4 & 5: Loop to create embeddings for each chunk
                for (int i = 0; i < splits; i++)
                {
                    int startIndex = i * splitLength;
                    int endIndex = Math.Min(startIndex + splitLength, documentText.Length);
                    string chunk = documentText.Substring(startIndex, endIndex - startIndex);

                    // Step 6: Generate embeddings
                    EmbeddingRequest embed = new EmbeddingRequest(Model.AdaTextEmbedding, chunk);
                    EmbeddingResult result = await api.Embeddings.CreateEmbeddingAsync(embed);
                    float[] embeddingVector = result;

                    // Step 7 & 8: Save embeddings and chunk text to files
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string chunkFileName = $"{originalFilenameWithoutExtension}_Chunk_{i + 1}_{timestamp}";

                    string chunkTextFilePath = Path.Combine(embeddingsFolder, chunkFileName + ".txt");
                    File.WriteAllText(chunkTextFilePath, chunk);

                    string vectorFilePath = Path.Combine(embeddingsFolder, chunkFileName + ".vect");
                    string vectorJson = JsonConvert.SerializeObject(embeddingVector);
                    File.WriteAllText(vectorFilePath, vectorJson);
                }

                Console.WriteLine("Done adding document to embeddings.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding document to embeddings: " + ex.Message);
                return false;
            }
        }

        public static String ReadDocument(string filePath)
        {
            try
            {
                string fileExtension = Path.GetExtension(filePath).ToLower();

                string extractedText = "";

                if (fileExtension == ".pdf")
                {
                    extractedText = OpenAIHelper.PdfFileToText(filePath);
                }
                else if (fileExtension == ".docx")
                {
                    extractedText = OpenAIHelper.ReadDocx(filePath);
                }
                else if (fileExtension == ".doc")
                {
                    extractedText = OpenAIHelper.ReadDoc(filePath);
                }
                else if (fileExtension == ".rtf")
                {
                    extractedText = OpenAIHelper.ReadRtf(filePath);
                }
                else
                {
                    MessageBox.Show("Unsupported file format");
                    return null;
                }

                return extractedText;
            }catch(Exception ex) 
            {
                return null;
            }
        }

        public static string PdfFileToText(string pdfFile)
        {
            StringBuilder text = new StringBuilder();

            try
            {
                using (PdfReader pdfReader = new PdfReader(pdfFile))
                {
                    using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                    {
                        for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                        {
                            PdfPage page = pdfDocument.GetPage(pageNum);
                            ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                            string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                            text.Append(pageText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine("Error extracting text from PDF: " + ex.Message);
            }

            return text.ToString();
        }
        public static string ReadDocx(string filePath)
        {
            using (DocX document = DocX.Load(filePath))
            {
                return document.Text;
            }
        }

        public static string ReadDoc(string filePath)
        {
            string text = string.Empty;

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                XWPFDocument doc = new XWPFDocument(stream);

                foreach (var paragraph in doc.Paragraphs)
                {
                    text += paragraph.ParagraphText + Environment.NewLine;
                }
            }

            return text;
        }



        public static string ReadRtf(string filePath)
        {
            // You need to implement a method to convert RTF to plain text
            // This might involve using a third-party library or a custom implementation
            // For the sake of simplicity, let's assume you have a method named ConvertRtfToText

            string rtfText = File.ReadAllText(filePath);
            string plainText = ConvertRtfToText(rtfText);

            return plainText;
        }

        public static string ConvertRtfToText(string rtf)
        {
            // Remove control words and curly braces
            string plainText = Regex.Replace(rtf, @"\\[^\\{}]+|{|}", "").Trim();

            // Replace line breaks with newline characters
            plainText = plainText.Replace("\\par", Environment.NewLine);
            return plainText;
        }



        public static async Task ProcessTextAndCreateFilesAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Console.WriteLine("Text is empty.");
                return;
            }
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string embeddingsFolderPath = Path.Combine(documentsPath, "AskYourDocs", "Embeddings");

            if (!Directory.Exists(embeddingsFolderPath))
            {
                Directory.CreateDirectory(embeddingsFolderPath);
            }

            OpenAIAPI api = new OpenAIAPI(OpenAIHelper.GetLicenseKey());

            // Split the text into 5000-character chunks.
            string[] chunks = SplitTextIntoChunks(text, 7000);

            for (int i = 0; i < chunks.Length; i++)
            {
                EmbeddingRequest embed = new EmbeddingRequest(Model.AdaTextEmbedding, chunks[i]);
                EmbeddingResult result = await api.Embeddings.CreateEmbeddingAsync(embed);

                float[] embeddingVector = result;

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string chunkFileName = $"Chunk_{i + 1}_{timestamp}";

                // Save the text chunk to a .txt file.
                string textChunkFilePath = Path.Combine(embeddingsFolderPath, chunkFileName + ".txt");
                File.WriteAllText(textChunkFilePath, chunks[i]);

                // Save the embedding vector to a .vect file.
                string vectorFilePath = Path.Combine(embeddingsFolderPath, chunkFileName + ".vect");
                string vectorJson = JsonConvert.SerializeObject(embeddingVector);
                File.WriteAllText(vectorFilePath, vectorJson);
            }

            Console.WriteLine("Done");
        }

        public static string[] SplitTextIntoChunks(string text, int chunkSize)
        {
            // Remove \r, \n, and \t individually from the text
            text = text.Replace("\r", "").Replace("\n", "").Replace("\t", "");

            return Enumerable.Range(0, (int)Math.Ceiling((double)text.Length / chunkSize))
                .Select(i => text.Substring(i * chunkSize, Math.Min(chunkSize, text.Length - i * chunkSize)))
                .ToArray();
        }

        public static async Task<double[]> ConvertQuestionToEmbedding(string vectorText)
        {
            OpenAIAPI api = new OpenAIAPI(OpenAIHelper.GetLicenseKey());
            try
            {
                EmbeddingRequest embed = new EmbeddingRequest(Model.AdaTextEmbedding, vectorText);
                EmbeddingResult result = await api.Embeddings.CreateEmbeddingAsync(embed);

                float[] embeddingVector = result;

                // Convert the float[] to double[]
                double[] doubleVector = embeddingVector.Select(f => (double)f).ToArray();
                return doubleVector;
            }
            catch (Exception ex)
            {
                // Handle conversion error
                return null;
            }
        }

        public static string PerformVectorComparison(string vectorDirectory, double[] vectorToCompare)
        {
            string[] vectorFiles = Directory.GetFiles(vectorDirectory, "*.vect");

            double minDifference = double.MaxValue;
            string mostSimilarFile = "";

            foreach (string vectorFile in vectorFiles)
            {
                double[] targetVector = LoadVectorsFromFile(vectorFile);

                if (targetVector != null)
                {
                    double similarity = CalculateCosineSimilarity(vectorToCompare, targetVector);
                    double difference = 1 - similarity;

                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        mostSimilarFile = Path.ChangeExtension(vectorFile, ".txt");  // Change extension to ".txt"
                    }
                }
            }

            return mostSimilarFile;
        }

        private static double[] LoadVectorsFromFile(string filePath)
        {
            try
            {

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    line = reader.ReadLine();

                    double[] vector = ParseVector(line);
                    return vector;

                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        static double CalculateCosineSimilarity(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must have the same length.");
            }

            double dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
            double magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            double magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0; // Handle division by zero
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        private static double[] ParseVector(string vectorString)
        {
            try
            {
                vectorString = vectorString.Trim('[', ']'); // Remove square brackets from the beginning and end
                var vectorValues = vectorString.Split(','); // Split the values by comma
                var vector = Array.ConvertAll(vectorValues, double.Parse); // Convert string values to doubles
                return vector;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<String> SearchEmbeddings(String embeddingsFolderPath,String questionStr)
        {
            double[] question = await ConvertQuestionToEmbedding(questionStr);

           
            // Find the most similar vector file
            string similarFile = PerformVectorComparison(embeddingsFolderPath, question);

            // Read the content from the similar text file and set it in textBox3
            string similarTextContent = File.ReadAllText(similarFile);
            return similarTextContent;
        }

        public static async Task<String> AskGPT(String question, String embeddings, String convHist)
        {
            String message = "Answer the question given below based on the embeddings given. Answer only if you are sure !!";
            message += "[Question=" + question + "]";
            message += "[Embeddings=" + embeddings + "]";
            if (convHist != null)
            {
                message += "[Chat History =" + embeddings + "]";
            }
            try
            {
                OpenAIAPI api = new OpenAIAPI(OpenAIHelper.GetLicenseKey());
                // Prepare the chat request
                var chatRequest = new ChatRequest()
                {
                    Model = Model.GPT4,
                    Temperature = 0.1,
                    MaxTokens = 2000,

                    Messages = new ChatMessage[] {
                        new ChatMessage(ChatMessageRole.User, message)
                    }
                };

                // Call the API to get the chat completion
                var result = await api.Chat.CreateChatCompletionAsync(chatRequest);

                // Get the AI reply from the response
                var reply = result.Choices[0].Message;
                return reply.Content.Trim();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the API call
                // For simplicity, we'll just display the error message in a message box
                return ex.Message;
            }
        }
    }
}
