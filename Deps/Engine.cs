using System;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using RestSharp;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading;
using System.ComponentModel;
using Windows.System.Threading;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Collections;

namespace Deps
{
    class Engine
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
         */

        public Engine()
        {

        }

        const string AccessToken = "53bedd9b-1a10-4ce3-b645-07638c19c0d2";
        const string Crs = "DHM";
        const int TimeWindow = 60;
        const int Rows = 20;
        internal static TimeSpan WALKING_TIME = TimeSpan.FromMinutes(6);
        internal static TimeSpan LAST_DRINK_TIME = TimeSpan.FromMinutes(15);
        internal static TimeSpan LAST_LOO_TIME = TimeSpan.FromMinutes(10);

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
                var client = new RestClient("https://lite.realtime.nationalrail.co.uk");
                var request = new RestRequest("OpenLDBWS/ldb9.asmx");
                request.RequestFormat = DataFormat.Xml;
                request.AddParameter("text/xml", xml, ParameterType.RequestBody);
                var res = await client.ExecutePostTaskAsync<XObject>(request);

                XmlReaderSettings settings = new XmlReaderSettings();
                //settings.ConformanceLevel = ConformanceLevel.Fragment;
                using (var reader = XmlReader.Create(new StringReader(res.Content)))
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

        public async Task InitLoad()
        {
            await GetBoard();
            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                Task.WaitAll(GetBoard());
            }, TimeSpan.FromMinutes(0.2));
        }
        
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
        public bool IsRunNow
        {
           get
           {
                return Expected.Subtract(DateTime.Now.TimeOfDay) > Engine.WALKING_TIME && Expected.Subtract(DateTime.Now.TimeOfDay) < (Engine.WALKING_TIME + TimeSpan.FromMinutes(5));
           }
        }

        public bool IsMissed
        {
            get
            {
                return Expected.Subtract(DateTime.Now.TimeOfDay) < Engine.WALKING_TIME;
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
            return serviceIDField == (obj as trainServicesService).serviceIDField && etd == (obj as trainServicesService).etd;
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
                try
                {
                    return TimeSpan.Parse(etdField);
                }
                catch
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