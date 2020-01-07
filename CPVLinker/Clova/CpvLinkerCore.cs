using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

namespace CPVLinker.Clova
{
    public delegate void RequestFinishedEventHandler(string requestedText, RequestResult requestResult);

    public class RequestResult
    {
        public bool IsSuccessed = false;
        public string Message = string.Empty;
        public string ResultPath = string.Empty;
    }

    public class CpvLinkerCore : IDisposable
    {
        public static string CPV_URL_BASE = "https://naveropenapi.apigw.ntruss.com";
        public static string CPV_URI = "/voice-premium/v1/tts";
        private static string CLIENT_ID_NAME = "X-NCP-APIGW-API-KEY-ID";
        private static string CLIENT_SECRET_NAME = "X-NCP-APIGW-API-KEY";

        public event RequestFinishedEventHandler RequestFinished;

        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        private string targetPath = "Voices\\";
        public string TargetDirectory {
            get { return targetPath; }
            set     //Target 경로를 바꿀 때마다 history 읽기
            {
                if(value[value.Length - 1] == '\\')
                {
                    targetPath = value;
                }
                else
                {
                    targetPath = value + "\\";
                }
                if(Directory.Exists(targetPath) == false)
                {
                    Directory.CreateDirectory(targetPath);
                }
                LoadHistory();
            }
        }
        private Dictionary<string, string> history = new Dictionary<string, string>();
        private BinaryFormatter serializer = new BinaryFormatter();

        public CpvLinkerCore(string clientId, string clientSecret)
        {
            ClientID = clientId;
            ClientSecret = clientSecret;
        }

        private void LoadHistory()
        {
            if (File.Exists(targetPath + "history.bin"))
            {
                using (FileStream stream = File.OpenRead(targetPath + "history.bin"))
                {
                    history = (Dictionary<string, string>)serializer.Deserialize(stream);
                }
            }
            else
            {
                Console.WriteLine("No history");
                history = new Dictionary<string, string>();
            }
        }

        public RequestResult RequestVoice(string text)
        {
            if(text.Length < 8)
            {
                return RequestVoice(text, "CPV_" + DateTime.Now.ToString("yyMMdd_HHmmss") + "_" + text.Substring(0, text.Length));
            }
            else
            {
                return RequestVoice(text, "CPV_" + DateTime.Now.ToString("yyMMdd_HHmmss") + "_" + text.Substring(0, 8));
            }
            
        }

        public RequestResult RequestVoice(string text, string name)
        {
            RequestResult result;       //Result는 요청 결과 값

            if (text.Length <= 0)
            {
                result = new RequestResult()
                {
                    IsSuccessed = false,
                    Message = "No Text"
                };
                RequestFinished?.Invoke(text, result);

                return result;
            }       //텍스트가 없으면 내부 오류로 처리

            string resultPath = TargetDirectory + Regex.Replace(name, @"[^a-zA-Z0-9가-힣]", "_");
            //확장자 불포함 파일 경로

            if (history.TryGetValue(text, out string path))
            {
                try
                {
                    File.Copy(path, TargetDirectory + name + "_cpy.mp3");
                    Debug.WriteLine("Voice File Copied in " + TargetDirectory);
                    result = new RequestResult()
                    {
                        IsSuccessed = true,
                        Message = "File Copied",
                        ResultPath = resultPath + "_cpy.mp3"
                    };
                    RequestFinished?.Invoke(text, result);

                    return result;      //이미 history.bin에 기록이 있는 경우 파일을 복사
                }
                catch(FileNotFoundException e)
                {
                    history.Remove(text);
                    Debug.WriteLine(e);
                }
            }

            string url = "https://naveropenapi.apigw.ntruss.com/voice-premium/v1/tts";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(CLIENT_ID_NAME, ClientID);
            request.Headers.Add(CLIENT_SECRET_NAME, ClientSecret);
            request.Method = "POST";
            byte[] byteDataParams = Encoding.UTF8.GetBytes("speaker=nara&volume=0&speed=0&pitch=0&emotion=0&format=mp3&text=" + text);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteDataParams.Length;
            Stream st = request.GetRequestStream();
            st.Write(byteDataParams, 0, byteDataParams.Length);
            st.Close();
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                System.Windows.MessageBox.Show("Error Occured(" + e.Status + ")\n\n" + e.Message, "Web Exception",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                result = new RequestResult()
                {
                    IsSuccessed = false,
                    Message = e.Message
                };
                RequestFinished?.Invoke(text, result);

                return result;
            }

            string status = response.StatusCode.ToString();
            Debug.Write("status=" + status + " --> ");
            using (Stream output = File.OpenWrite(resultPath + ".mp3"))
            using (Stream input = response.GetResponseStream())
                input.CopyTo(output);

            response.Dispose();

            if (history.ContainsKey(text) == false)
            {
                history.Add(text, resultPath + ".mp3");
            }

            //---- 네이버 클라우드 서버에 CPV 요청 및 파일 반환 완료 ----//

            Debug.WriteLine("Voice File Created in " + resultPath + ".mp3");

            result = new RequestResult()
            {
                IsSuccessed = true,
                Message = "File Created",
                ResultPath = resultPath + ".mp3"
            };
            RequestFinished?.Invoke(text, result);

            return result;
        }

        public void RequstVoiceFromCSV(string path)
        {
            RequstVoiceFromCSV(path, 0, int.MaxValue);
        }

        public virtual void RequstVoiceFromCSV(string path, int start, int end)
        {
            if (File.Exists(path))
            {
                string log = string.Empty;
                string endResultMessage = "All converting work complete";

                using (FileStream stream = File.OpenRead(path))
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false))
                {
                    int count = -1;
                    string dataLine = string.Empty;
                    while((dataLine = reader.ReadLine()) != null)
                    {
                        count++;

                        if(count < start)
                        {
                            log += "Passed!\r\n";
                            continue;
                        }
                        else if(count > end)
                        {
                            log += "Passed!\r\n";
                            continue;
                        }

                        log += "Line " + count + "...";

                        //가능하면 TextFieldParser를 사용한 새로운 알고리즘으로 대체 필요
                        StringReader dataLineReader = new StringReader(dataLine);
                        TextFieldParser parser = new TextFieldParser(dataLineReader)
                        {
                            HasFieldsEnclosedInQuotes = true
                        };
                        parser.SetDelimiters(",");

                        string[] subData = parser.ReadFields();

                        if (int.TryParse(subData[0], out int sceneNumber) == false)
                        {
                            log += "Not Number!\r\n";
                            continue;
                        }
                        else
                        {
                            if (subData.Length < 5)      //유효성 검증
                            {
                                endResultMessage = "The number of data in the line is too short\n\rData is corrupted";
                                log += endResultMessage;
                                break;

                            }
                            else if (subData.Length > 5)
                            {
                                endResultMessage = "The number of data in the line is too many\n\rData is corrupted";
                                log += endResultMessage;
                                break;
                            }
                            else        //정상
                            {
                                Debug.WriteLine("Cnt: " + count + " ");
                                RequestResult result = RequestVoice(subData[3], GetFormattedName(sceneNumber, subData[3], count, "1").Replace('/', '-'));
                                if(result.IsSuccessed == false)
                                {
                                    log += endResultMessage;
                                    break;
                                }
                                log += "1st voice..." + result.Message + "!, ";
                                
                                result = RequestVoice(subData[4], GetFormattedName(sceneNumber, subData[4], count, "2").Replace('/', '-'));
                                if (result.IsSuccessed == false)
                                {
                                    log += endResultMessage;
                                    break;
                                }
                                log += "2nd voice..." + result.Message + "!";
                                Debug.WriteLine("");
                            }
                        }

                        log += "\r\n";
                    }
                }

                using (FileStream stream = new FileStream(TargetDirectory + "/log.txt", FileMode.Create))
                {
                    stream.Write(Encoding.UTF8.GetBytes(log), 0, log.Length);
                }
                System.Windows.MessageBox.Show(endResultMessage, "Result");
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        private string GetFormattedName(int sceneNumber, string content, int countNum, string mark)
        {
            if (content.Length < 8)
            {
                return "CPV_sc" + Get10DigitAdded(sceneNumber) + "_" + Get100DigitAdded(countNum) + " [" + mark + "] " + DateTime.Now.ToString("yyMMdd_HHmmss") + "_" + content.Substring(0, content.Length);
            }
            else
            {
                return "CPV_sc" + Get10DigitAdded(sceneNumber) + "_" + Get100DigitAdded(countNum) + " [" + mark + "] " + DateTime.Now.ToString("yyMMdd_HHmmss") + "_" + content.Substring(0, 8);
            }
        }

        private string Get10DigitAdded(int number)
        {
            if(number < 10)
            {
                return "0" + number.ToString();
            }
            else
            {
                return number.ToString();
            }
        }

        private string Get100DigitAdded(int number)
        {
            if(number < 100)
            {
                return "0" + Get10DigitAdded(number);
            }
            else
            {
                return number.ToString();
            }
        }

        public void Dispose()
        {
            FileStream stream = new FileStream(targetPath + "history.bin", FileMode.Create);
            serializer.Serialize(stream, history);
        }
    }
}


/* 이전 코드( 참조용 )
 * 

        public class CpvLinkerCore : IDisposable
    {
        public static string CPV_URL_BASE = "https://naveropenapi.apigw.ntruss.com";
        public static string CPV_URI = "/voice-premium/v1/tts";

        private HttpClient client = new HttpClient();

        private static string CLIENT_ID_NAME = "X-NCP-APIGW-API-KEY-ID";
        private static string CLIENT_SECRET_NAME = "X-NCP-APIGW-API-KEY";
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        private Dictionary<string, string> voiceOption = new Dictionary<string, string>();

        public CpvLinkerCore(string clientId, string clientSecret)
        {
            ClientID = clientId;
            ClientSecret = clientSecret;
            client.BaseAddress = new Uri(CPV_URL_BASE);

            InitializeOption();
        }

        private void InitializeOption()
        {
            voiceOption.Add("speaker", "nara");
            voiceOption.Add("volume", "0");
            voiceOption.Add("speed", "0");
            voiceOption.Add("pitch", "0");
            voiceOption.Add("emotion", "0");
            voiceOption.Add("format", "mp3");
        }

        public async void RequestVoice(string text)
        {
            voiceOption.Add("text", text);
            string option = JsonConvert.SerializeObject(voiceOption);
            
            StringContent content = new StringContent(option, Encoding.UTF8, "application/x-www-form-urlencoded");
            content.Headers.Add(CLIENT_ID_NAME, ClientID);
            content.Headers.Add(CLIENT_SECRET_NAME, ClientSecret);

            content.Headers.Add("form", option);
            content.Headers.Add("headers", "{ " + CLIENT_ID_NAME + ": " + ClientID + ", " + CLIENT_SECRET_NAME + ": " + ClientSecret + " }");

            HttpResponseMessage response = await client.PostAsync(CPV_URI, content);

            Debug.WriteLine(response.Content.Headers);
            Debug.WriteLine(response.Headers);
            Debug.WriteLine(response.RequestMessage.Headers);
            Debug.WriteLine(response.RequestMessage.Content.Headers);
            Debug.WriteLine(response.StatusCode);

            using (Stream output = File.OpenWrite("C:/temp/tts.mp3"))
            using (Stream input = await response.Content.ReadAsStreamAsync())
            {
                input.CopyTo(output);
            }

            Debug.WriteLine("Done");
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }


 */
