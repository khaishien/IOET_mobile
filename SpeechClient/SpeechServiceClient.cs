using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace CaregiverMobile.SpeechClient
{
    public class SpeechServiceClient
    {
        //private HttpClient _httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler()));
        private HttpClient _httpClient = new HttpClient();

        private const string _VERIFICATION_PROFILE_URI = "https://api.projectoxford.ai/spid/v1.0/verificationProfiles";
        private const string _VERIFICATION_URI = "https://api.projectoxford.ai/spid/v1.0/verify?verificationProfileId=";
        private const string _VERIFICATION_PHRASE_URI = "https://api.projectoxford.ai/spid/v1.0/verificationPhrases?locale=en-us";

        private const string _SUBSCRIPTION_KEY_HEADER = "Ocp-Apim-Subscription-Key";
        private const string _JSON_CONTENT_HEADER_VALUE = "application/json";
        private const string _AUDIO_CONTENT_HEADER_VALUE = "multipart/form-data";
        private const string _OPERATION_LOCATION_HEADER = "Operation-Location";
        private static CamelCasePropertyNamesContractResolver s_defaultResolver = new CamelCasePropertyNamesContractResolver();

        private static JsonSerializerSettings s_jsonDateTimeSettings = new JsonSerializerSettings()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = s_defaultResolver
        };

        public SpeechServiceClient(string subscriptionKey)
        {
            //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_JSON_CONTENT_HEADER_VALUE));
            _httpClient.DefaultRequestHeaders.Add(_SUBSCRIPTION_KEY_HEADER, subscriptionKey);
        }

        public async Task<CreateProfileResponse> CreateProfileAsync(string locale)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("locale", locale),
            });

            HttpResponseMessage response = await _httpClient.PostAsync(_VERIFICATION_PROFILE_URI, content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //parse response
                string resultStr = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CreateProfileResponse>(resultStr);
            }
            else
            {
                throw new ProfileCreationException(response.ReasonPhrase);
            }
        }

        public async Task<EnrollmentResponse> EnrollAsync(IInputStream audioStream, string profileId, TimeSpan retryDelay, int numberOfRetries)
        {

            //HttpClient httpClient = new HttpClient();
            //_httpClient.DefaultRequestHeaders.Add(_SUBSCRIPTION_KEY_HEADER, "d14f2a5acaa64e00bf0732b6bd557d59");
            //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_AUDIO_CONTENT_HEADER_VALUE));

            //int offset;
            //byte[] bits = this.HackOxfordWavPcmStream(audioStream, out offset);
            //Debug.WriteLine("bits:" + Encoding.UTF8.GetString(bits));

            //ByteArrayContent content = new ByteArrayContent(bits, offset, bits.Length - offset);
            var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString("u"));
            content.Add(new StreamContent(audioStream.AsStreamForRead()), "enrollmentData", profileId + "_" + DateTime.Now.ToString("u"));

            profileId = Uri.EscapeDataString(profileId);
            string requestUri = _VERIFICATION_PROFILE_URI + "/" + profileId + "/enroll";
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // parse response
                string resultStr = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<EnrollmentResponse>(resultStr);
            }
            else if (response.StatusCode == HttpStatusCode.Accepted)
            {
                IEnumerable<string> operationLocation = response.Headers.GetValues(_OPERATION_LOCATION_HEADER);
                if (operationLocation.Count() == 1)
                {
                    string operationUrl = operationLocation.First();

                    // Send the request
                    EnrollmentOperationResponse operationResponse;
                    while (numberOfRetries-- > 0)
                    {
                        await Task.Delay(retryDelay);

                        response = await _httpClient.GetAsync(operationUrl);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            // parse response
                            string resultStr = await response.Content.ReadAsStringAsync();
                            operationResponse = JsonConvert.DeserializeObject<EnrollmentOperationResponse>(resultStr, s_jsonDateTimeSettings);
                        }
                        else
                        {
                            throw new EnrollmentException(response.ReasonPhrase);
                        }

                        if (operationResponse.Status == OperationStatus.Succeeded)
                        {
                            return operationResponse.ProcessingResult;
                        }
                        else if (operationResponse.Status == OperationStatus.Failed)
                        {
                            throw new EnrollmentException(operationResponse.Message);
                        }
                    }

                    throw new EnrollmentException("Polling on operation status timed out");
                }
                else
                {
                    throw new EnrollmentException("Incorrect server response");
                }
            }
            else
            {
                throw new EnrollmentException(response.ReasonPhrase);
            }

        }

        private byte[] HackOxfordWavPcmStream(IInputStream audioStream, out int offset)
        {
            var netStream = audioStream.AsStreamForRead();
            var bits = new byte[netStream.Length];
            netStream.Read(bits, 0, bits.Length);

            // original file length
            var pcmFileLength = BitConverter.ToInt32(bits, 4);

            // take away 36 bytes for the JUNK chunk
            pcmFileLength -= 36;

            // now copy 12 bytes from start of bytes to 36 bytes further on
            for (int i = 0; i < 12; i++)
            {
                bits[i + 36] = bits[i];
            }
            // now put modified file length into byts 40-43
            var newLengthBits = BitConverter.GetBytes(pcmFileLength);
            newLengthBits.CopyTo(bits, 40);

            // the bits that we want are now 36 onwards in this array
            offset = 36;

            return (bits);
        }

        public async Task<VerificationProfile> GetProfileAsync(string profileId)
        {
            profileId = Uri.EscapeDataString(profileId);
            string requestUri = _VERIFICATION_PROFILE_URI + "/" + profileId;

            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //parse response
                string resultStr = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<VerificationProfile>(resultStr, s_jsonDateTimeSettings);
            }
            else
            {
                throw new GetProfileException(response.ReasonPhrase);
            }
        }

        public async Task<List<VerificationPhrase>> GetVerficationPhrase()
        {
            string requestUri = _VERIFICATION_PHRASE_URI;

            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //parse response
                string resultStr = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<VerificationPhrase>>(resultStr, s_jsonDateTimeSettings);
            }
            else
            {
                throw new GetPhraseException(response.ReasonPhrase);
            }
        }

        public async Task<VerificationResponse> VerifyAsync(IInputStream audioStream, string testProfileIds, TimeSpan retryDelay, int numberOfRetries)
        {
            var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString("u"));
            content.Add(new StreamContent(audioStream.AsStreamForRead()), "verificationData", "testFile_" + DateTime.Now.ToString("u"));

            string testProfileIdsString = testProfileIds;
            string requestUri = _VERIFICATION_URI + Uri.EscapeDataString(testProfileIdsString);

            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //parse response
                string resultStr = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<VerificationResponse>(resultStr);
            }
            else if (response.StatusCode == HttpStatusCode.Accepted)
            {
                IEnumerable<string> operationLocation = response.Headers.GetValues(_OPERATION_LOCATION_HEADER);
                if (operationLocation.Count() == 1)
                {
                    string operationUrl = operationLocation.First();

                    // Send the request
                    VerificationOperationResponse operationResponse;
                    while (numberOfRetries-- > 0)
                    {
                        await Task.Delay(retryDelay);
                        response = await _httpClient.GetAsync(operationUrl);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            // parse response
                            string resultStr = await response.Content.ReadAsStringAsync();
                            operationResponse = JsonConvert.DeserializeObject<VerificationOperationResponse>(resultStr, s_jsonDateTimeSettings);
                        }
                        else
                        {
                            throw new VerificationException(response.ReasonPhrase);
                        }

                        if (operationResponse.Status == OperationStatus.Succeeded)
                        {
                            return operationResponse.ProcessingResult;
                        }
                        else if (operationResponse.Status == OperationStatus.Failed)
                        {
                            throw new VerificationException(operationResponse.Message);
                        }
                    }

                    throw new VerificationException("Polling on operation status timed out");
                }
                else
                {
                    throw new VerificationException("Incorrect server response");
                }
            }
            else
            {
                throw new VerificationException(response.ReasonPhrase);
            }
        }


        public async Task<Boolean> ResetEnrollAsync(string profileId)
        {
            profileId = Uri.EscapeDataString(profileId);
            string requestUri = _VERIFICATION_PROFILE_URI + "/" + profileId + "/reset";
            var response = await _httpClient.PostAsync(requestUri,null);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //parse response
                //string resultStr = await response.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                throw new ResetEnrollException(response.ReasonPhrase);
            }
        }

    }
}
