using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace DepartureBoard
{
    public sealed class AppSettings
    {
        public string DARKSKY_API_KEY { get;set; }       // 
        public string RAIL_API_KEY { get; set; }        // =
        public string STATION_CRS { get; set; }         // = "DHM";
        public double LOCATION_LAT { get; set; }        // = 54.7794;
        public double LOCATION_LNG { get; set; }        // = -1.5817600000000311;
        public int GET_DRINK_TIME { get; set; }         // = 26;
        public int DRINK_UP_TIME { get; set; }          // = 16;
        public int WALK_TIME_TO_STATION { get; set; }   // = 10;
        public int FONT_SIZE { get; set; }

        private void Load()
        {
            DARKSKY_API_KEY = ApplicationData.Current.LocalSettings.Values["DARKSKY_API_KEY"] as string;
            RAIL_API_KEY = ApplicationData.Current.LocalSettings.Values["RAIL_API_KEY"] as string;
            STATION_CRS = ApplicationData.Current.LocalSettings.Values["STATION_CRS"] as string;

            try
            {
                LOCATION_LAT = double.Parse(ApplicationData.Current.LocalSettings.Values["LOCATION_LAT"].ToString());
                LOCATION_LNG = double.Parse(ApplicationData.Current.LocalSettings.Values["LOCATION_LNG"].ToString());
            }
            catch { }
            try
            {
                FONT_SIZE = int.Parse(ApplicationData.Current.LocalSettings.Values["FONT_SIZE"].ToString());
            }
            catch
            {
                FONT_SIZE = 56;
            }
            try
            {
                GET_DRINK_TIME = (int)ApplicationData.Current.LocalSettings.Values["GET_DRINK_TIME"];
            }
            catch {
                GET_DRINK_TIME = 26;
            }
            try
            {
                DRINK_UP_TIME = (int)ApplicationData.Current.LocalSettings.Values["DRINK_UP_TIME"];
            }
            catch {
                DRINK_UP_TIME = 16;
            }
            try
            {
                WALK_TIME_TO_STATION = (int)ApplicationData.Current.LocalSettings.Values["WALK_TIME_TO_STATION"];
            }
            catch {
                WALK_TIME_TO_STATION = 10;
            }
        }

        internal void Save()
        {
            ApplicationData.Current.LocalSettings.Values["DARKSKY_API_KEY"] = this.DARKSKY_API_KEY;
            ApplicationData.Current.LocalSettings.Values["RAIL_API_KEY"] = this.RAIL_API_KEY;
            ApplicationData.Current.LocalSettings.Values["STATION_CRS"] = this.STATION_CRS;
            ApplicationData.Current.LocalSettings.Values["LOCATION_LAT"] = this.LOCATION_LAT;
            ApplicationData.Current.LocalSettings.Values["LOCATION_LNG"] = this.LOCATION_LNG;
            ApplicationData.Current.LocalSettings.Values["GET_DRINK_TIME"] = this.GET_DRINK_TIME;
            ApplicationData.Current.LocalSettings.Values["DRINK_UP_TIME"] = this.DRINK_UP_TIME;
            ApplicationData.Current.LocalSettings.Values["WALK_TIME_TO_STATION"] = this.WALK_TIME_TO_STATION;
            ApplicationData.Current.LocalSettings.Values["FONT_SIZE"] = this.FONT_SIZE;

            OnSettingsChanged?.Invoke();
        }

        public int RUN_TIME_TO_STATION { set; get; }    // = 8;
        public int TOO_LATE { get; set; }               // = 6;
        public int TIME_WINDOW { get; set; }            // = 70;
        public int MAX_ROWS { get; set; }               // = 10;

        public event Action OnSettingsChanged = delegate { };

        private static AppSettings _current;
        public static AppSettings Current
        {
            get {
                if (_current == null)
                {
                    _current = new AppSettings();
                    _current.Load();
                    //_current.RAIL_API_KEY = "change me";
                }
                return _current;
            }
        }
    }
}
