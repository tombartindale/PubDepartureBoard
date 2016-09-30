using System;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using Windows.System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using ForecastIOPortable;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Deps
{
    public class Engine
    {
        /*
         * Ideas:
         * - time for toilet break
         * - run  to station now
         * - time for another beer
         * - drink up
         * - another train to same place in ? mins
         * - last train
         * - weather (i.e. its raining)
         * - next train north (rotating display)
         * - next train south 
         * platform (over the bridge)
         */
        
        public enum BoardStatus:int {TOOLATE = 6, RUNNOW = 8,GONOW=10, DRINKUP = 16, GETDRINK = 26, NORMAL };

        ForecastApi weather = new ForecastApi("91a059370a899851a4aa05a290ff0f4b");
        double lat = 54.7794;
        double lng = -1.5817600000000311;

        public Engine()
        {
            
        }

        const string AccessToken = "53bedd9b-1a10-4ce3-b645-07638c19c0d2";
        const string Crs = "DHM";
        const int TimeWindow = 70;
        const int Rows = 20;

        private async Task GetBoard()
        {
            try
            {
                string xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                $"<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ns1=\"http://thalesgroup.com/RTTI/2016-02-16/ldb/\">" +
                $"  <SOAP-ENV:Header xmlns:ns2=\"http://thalesgroup.com/RTTI/2010-11-01/ldb/commontypes\">" +
                $"    <ns2:AccessToken><ns2:TokenValue>{AccessToken}</ns2:TokenValue></ns2:AccessToken>" +
                $"  </SOAP-ENV:Header>" +
                $"  <SOAP-ENV:Body>" +
                $"    <ns1:GetDepBoardWithDetailsRequest>" +
                $"      <ns1:numRows>{Rows}</ns1:numRows>" +
                $"      <ns1:crs>{Crs}</ns1:crs>" +
                $"      <ns1:filterCrs></ns1:filterCrs>" +
                $"      <ns1:filterType>from</ns1:filterType>" +
                $"      <ns1:timeOffset>0</ns1:timeOffset>" +
                $"      <ns1:timeWindow>{TimeWindow}</ns1:timeWindow>" +
                $"    </ns1:GetDepBoardWithDetailsRequest>" +
                $"  </SOAP-ENV:Body>" +
                $"</SOAP-ENV:Envelope>";

                string res;
                HttpResponseMessage result;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://lite.realtime.nationalrail.co.uk");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    result = await client.PostAsync("/OpenLDBWS/ldb9.asmx", new StringContent(xml,Encoding.UTF8,"text/xml"));
                }

                res = await result.Content.ReadAsStringAsync();

                    //var res = await "https://lite.realtime.nationalrail.co.uk/OpenLDBWS/ldb9.asmx"
                    ////.AppendPathSegments("OpenLDBWS","ldb9.asmx")
                    //.WithHeader("Accept", "text/xml")
                    ////.WithHeader("Content-Type","text/xml")
                    //.PostStringAsync(xml)
                    //.ReceiveString();
                    //var client = new RestClient();
                    //client.BaseUrl = "https://lite.realtime.nationalrail.co.uk";
                    //var request = new RestRequest("/OpenLDBWS/ldb9.asmx",HttpMethod.Post);
                    //request.ContentType = ContentTypes.ByteArray;
                    //request.IgnoreXmlAttributes = true;
                    //request.AddParameter(Encoding.UTF8.GetBytes(xml));
                    //request.ReturnRawString = true;
                    //request.RequestFormat = DataFormat.Xml;
                    //request.AddParameter("text/xml", xml, ParameterType.RequestBody);
                    //request.Method = Method.POST;

                    //var res = await client.ExecuteAsync<string>(request);

                    XmlReaderSettings settings = new XmlReaderSettings();
                //settings.ConformanceLevel = ConformanceLevel.Fragment;
                using (var reader = XmlReader.Create(new StringReader(res)))
                {
                    Message m = Message.CreateMessage(reader, int.MaxValue, MessageVersion.Soap11);
                    string contents = m.GetReaderAtBodyContents().ReadInnerXml();
                    contents = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + contents;
                    XmlSerializer der = new XmlSerializer(typeof(GetStationBoardResult), "http://thalesgroup.com/RTTI/2016-02-16/ldb/");
                    GetStationBoardResult obj = der.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(contents))) as GetStationBoardResult;
                    CurrentTrains = new ObservableCollection<trainServicesService>(obj.trainServices);
                    if (_currentboard != null && !_currentboard.trainServices.SequenceEqual(obj.trainServices))
                        OnUpdate?.Invoke();

                    _currentboard = obj;
                    OnLatestData?.Invoke(_currentboard.generatedAt);
                }
            }
            catch (Exception e)
            {
                //failed to load data
                Debug.WriteLine("Failed to load data " + e.Message);
            }

        }

        public event Action<DateTime> OnLatestData;

        private GetStationBoardResult  _currentboard;

        public GetStationBoardResult CurrentBoard
        {
            get
            {
                return _currentboard;
            }
        }

        public ObservableCollection<trainServicesService> CurrentTrains { get; private set; }
        public event Action OnUpdate;
        public event Action<string> OnWeatherUpdate;

        public async Task InitLoad()
        {
            await GetBoard();
            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                Task.WaitAll(GetBoard());
            }, TimeSpan.FromMinutes(0.2));

            await GetWeather();
            ThreadPoolTimer weather_timer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                Task.WaitAll(GetWeather());
            }, TimeSpan.FromMinutes(5));
        }

        private async Task GetWeather()
        {
            var current_weather = await weather.GetWeatherDataAsync(lat, lng, Unit.UK, Language.English);
            LastWeatherMinutely = current_weather.Minutely;
            OnWeatherUpdate?.Invoke(current_weather.Currently.Icon);
        }

        public static ForecastIOPortable.Models.MinutelyForecast LastWeatherMinutely { get; private set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/", IsNullable = false)]
    public partial class GetStationBoardResult
    {

        private System.DateTime generatedAtField;


        private string locationNameField;

        private string crsField;

        private bool platformAvailableField;

        private trainServicesService[] trainServicesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public System.DateTime generatedAt
        {
            get
            {
                return generatedAtField;
                //return (DateTime)GetValue(GeneratedAt);
            }
            set
            {
                generatedAtField = value;
                //SetValue(GeneratedAt, value);
            }
        }

        //public static DependencyProperty GeneratedAt = DependencyProperty.Register("GeneratedAt", typeof(DateTime), typeof(GetStationBoardResult),null);


        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string locationName
        {
            get
            {
                return this.locationNameField;
            }
            set
            {
                this.locationNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string crs
        {
            get
            {
                return this.crsField;
            }
            set
            {
                this.crsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public bool platformAvailable
        {
            get
            {
                return this.platformAvailableField;
            }
            set
            {
                this.platformAvailableField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
        [System.Xml.Serialization.XmlArrayItemAttribute("service", IsNullable = false)]
        public trainServicesService[] trainServices
        {
            get
            {
                return this.trainServicesField;
            }
            set
            {
                this.trainServicesField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
    public partial class trainServicesService
    {
        //public object StatusIcon
        //{
        //    get
        //    {
        //        BitmapImage image = new BitmapImage();

        //        try
        //        {
        //            //image.BeginInit();
        //            //image.CacheOption = BitmapCacheOption.OnLoad;
        //            //image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        //            image.UriSource = new Uri(CurrentStatus.ToString().ToLower());
        //            //image.EndInit();
        //        }
        //        catch
        //        {
        //            return DependencyProperty.UnsetValue;
        //        }

        //        return image;
        //    }
        //}

        public TimeSpan TimeLeft
        {
            get
            {
                return Expected.Subtract(DateTime.Now.TimeOfDay) - TimeSpan.FromMinutes((int)Engine.BoardStatus.TOOLATE);
            }
        }

        public TimeSpan TimeLeftActual
        {
            get
            {
                return Expected.Subtract(DateTime.Now.TimeOfDay);
            }
        }

        public string Weather
        {
            get
            {
                int mins = (int)Expected.Subtract(DateTime.Now.TimeOfDay).Minutes;
                if (mins < 60 && mins > 0 && Engine.LastWeatherMinutely?.Minutes[mins].PrecipitationProbability  > 0)
                    return $"{Engine.LastWeatherMinutely?.Minutes[mins].PrecipitationProbability*100}% chance of {Engine.LastWeatherMinutely?.Minutes[mins].PrecipitationType}";
                else
                    return null;
            }
        }

        // TOOLATE = 6, RUNNOW = 8, DRINKUP = 16, LOOBREAK = 20, GETDRINK = 26
        public Engine.BoardStatus CurrentStatus
        {
            get
            {
                var timeleft = Expected.Subtract(DateTime.Now.TimeOfDay);
                if (timeleft < TimeSpan.FromMinutes((int)Engine.BoardStatus.TOOLATE))
                {
                    return Engine.BoardStatus.TOOLATE;
                }
                else if (timeleft < TimeSpan.FromMinutes((int)Engine.BoardStatus.RUNNOW))
                {
                    return Engine.BoardStatus.RUNNOW;
                }
                else if (timeleft < TimeSpan.FromMinutes((int)Engine.BoardStatus.GONOW))
                {
                    return Engine.BoardStatus.GONOW;
                }
                else if (timeleft < TimeSpan.FromMinutes((int)Engine.BoardStatus.DRINKUP))
                {
                    return Engine.BoardStatus.DRINKUP;
                }
                else if (timeleft < TimeSpan.FromMinutes((int)Engine.BoardStatus.GETDRINK))
                {
                    return Engine.BoardStatus.GETDRINK;
                }
                else
                {
                    return Engine.BoardStatus.NORMAL;
                }
            }
        }

        public bool IsTooLate
        {
            get
            {
                return CurrentStatus == Engine.BoardStatus.TOOLATE;
            }
        }

        public bool IsLate
        {
            get
            {
                return Due != Expected;
            }
        }

        public override bool Equals(object obj)
        {
            return serviceIDField == (obj as trainServicesService).serviceIDField && etd == (obj as trainServicesService).etd && CurrentStatus== (obj as trainServicesService).CurrentStatus && IsLate == (obj as trainServicesService).IsLate && TimeLeft == (obj as trainServicesService).TimeLeft && TimeLeftActual == (obj as trainServicesService).TimeLeftActual;
        }

        public override int GetHashCode()
        {
            return serviceIDField.GetHashCode() + etd.GetHashCode();
        }

        public TimeSpan Due
        {
            get
            {
                return TimeSpan.Parse(stdField);
            }
        }

        public TimeSpan Expected
        {
            get
            {
                TimeSpan res;
                if (TimeSpan.TryParse(etdField, out res))
                {
                    return res;
                }
                else
                { 
                    return Due;
                }
            }
        }



        private string stdField;

        private string etdField;

        private byte platformField;

        private string operatorField;

        private string operatorCodeField;

        private string serviceTypeField;

        private string cancelReasonField;

        private string delayReasonField;

        private string serviceIDField;

        private string rsidField;

        private trainServicesServiceOrigin originField;

        private trainServicesServiceDestination destinationField;

        private trainServicesServiceSubsequentCallingPoints subsequentCallingPointsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string std
        {
            get
            {
                return this.stdField;
            }
            set
            {
                this.stdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string etd
        {
            get
            {
                return this.etdField;
            }
            set
            {
                this.etdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public byte platform
        {
            get
            {
                return this.platformField;
            }
            set
            {
                this.platformField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string @operator
        {
            get
            {
                return this.operatorField;
            }
            set
            {
                this.operatorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string operatorCode
        {
            get
            {
                return this.operatorCodeField;
            }
            set
            {
                this.operatorCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string serviceType
        {
            get
            {
                return this.serviceTypeField;
            }
            set
            {
                this.serviceTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string cancelReason
        {
            get
            {
                return this.cancelReasonField;
            }
            set
            {
                this.cancelReasonField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string delayReason
        {
            get
            {
                return this.delayReasonField;
            }
            set
            {
                this.delayReasonField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public string serviceID
        {
            get
            {
                return this.serviceIDField;
            }
            set
            {
                this.serviceIDField = value;
            }
        }

        /// <remarks/>
        public string rsid
        {
            get
            {
                return this.rsidField;
            }
            set
            {
                this.rsidField = value;
            }
        }

        /// <remarks/>
        public trainServicesServiceOrigin origin
        {
            get
            {
                return this.originField;
            }
            set
            {
                this.originField = value;
            }
        }

        /// <remarks/>
        public trainServicesServiceDestination destination
        {
            get
            {
                return this.destinationField;
            }
            set
            {
                this.destinationField = value;
            }
        }

        /// <remarks/>
        public trainServicesServiceSubsequentCallingPoints subsequentCallingPoints
        {
            get
            {
                return this.subsequentCallingPointsField;
            }
            set
            {
                this.subsequentCallingPointsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
    public partial class trainServicesServiceOrigin
    {

        private location locationField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public location location
        {
            get
            {
                return this.locationField;
            }
            set
            {
                this.locationField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types", IsNullable = false)]
    public partial class location
    {

        private string locationNameField;

        private string crsField;

        /// <remarks/>
        public string locationName
        {
            get
            {
                return this.locationNameField;
            }
            set
            {
                this.locationNameField = value;
            }
        }

        /// <remarks/>
        public string crs
        {
            get
            {
                return this.crsField;
            }
            set
            {
                this.crsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
    public partial class trainServicesServiceDestination
    {

        private location locationField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        public location location
        {
            get
            {
                return this.locationField;
            }
            set
            {
                this.locationField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
    public partial class trainServicesServiceSubsequentCallingPoints
    {
        public override string ToString()
        {
            return string.Join(", ", callingPointList.ToList());
        }

        private callingPointListCallingPoint[] callingPointListField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
        [System.Xml.Serialization.XmlArrayItemAttribute("callingPoint", IsNullable = false)]
        public callingPointListCallingPoint[] callingPointList
        {
            get
            {
                return this.callingPointListField;
            }
            set
            {
                this.callingPointListField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
    public partial class callingPointListCallingPoint
    {

        public override string ToString()
        {
            return locationNameField;
        }

        private string locationNameField;

        private string crsField;

        private string stField;

        private string etField;

        /// <remarks/>
        public string locationName
        {
            get
            {
                return this.locationNameField;
            }
            set
            {
                this.locationNameField = value;
            }
        }

        /// <remarks/>
        public string crs
        {
            get
            {
                return this.crsField;
            }
            set
            {
                this.crsField = value;
            }
        }

        /// <remarks/>
        public string st
        {
            get
            {
                return this.stField;
            }
            set
            {
                this.stField = value;
            }
        }

        /// <remarks/>
        public string et
        {
            get
            {
                return this.etField;
            }
            set
            {
                this.etField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://thalesgroup.com/RTTI/2015-11-27/ldb/types", IsNullable = false)]
    public partial class callingPointList
    {

        private callingPointListCallingPoint[] callingPointField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("callingPoint")]
        public callingPointListCallingPoint[] callingPoint
        {
            get
            {
                return this.callingPointField;
            }
            set
            {
                this.callingPointField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://thalesgroup.com/RTTI/2016-02-16/ldb/types", IsNullable = false)]
    public partial class trainServices
    {

        private trainServicesService[] serviceField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("service")]
        public trainServicesService[] service
        {
            get
            {
                return this.serviceField;
            }
            set
            {
                this.serviceField = value;
            }
        }
    }


}