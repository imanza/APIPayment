using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Constants
{
    public class DateFuncs
    {
        #region Date

        public static DateTime ConvertStringToDate(string indate)
        {

            char[] delimitters = { '/' };
            string[] dateSections = indate.Split(delimitters, 3);
            DateTime result = new DateTime(Convert.ToInt32(dateSections[0]), Convert.ToInt32(dateSections[1]), Convert.ToInt32(dateSections[2]));
            return result;
        }

        public static DateTime ToGregorianDate(DateTime Shamsi)
        {
            System.Globalization.PersianCalendar persiancal = new System.Globalization.PersianCalendar();
            DateTime pdt = new DateTime(Shamsi.Year, Shamsi.Month, Shamsi.Day, persiancal);
            return pdt;
        }

        public static DateTime ToShamsiCal(DateTime? gregorianDatenullable)
        {
            System.Globalization.PersianCalendar currentPersianDate = new System.Globalization.PersianCalendar();
            if (gregorianDatenullable == null) return currentPersianDate.MinSupportedDateTime;
            DateTime gregorianDate = (DateTime)gregorianDatenullable;
            try
            {
                return new DateTime(currentPersianDate.GetYear(gregorianDate), currentPersianDate.GetMonth(gregorianDate), currentPersianDate.GetDayOfMonth(gregorianDate), currentPersianDate.GetHour(gregorianDate), currentPersianDate.GetMinute(gregorianDate), 0);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return currentPersianDate.MinSupportedDateTime;
            }
        }

        public static string Get_String_Date(DateTime gcal)
        {
            string m = "" + gcal.Month, d = "" + gcal.Day;
            if (gcal.Month < 10)
            {
                m = "0" + m;
            }
            if (gcal.Day < 10)
            {
                d = "0" + d;
            }
            return "" + d + " / " + m + " / " + gcal.Year;
        }

        public static string Get_Full_String_Date(DateTime gcal)
        {
            string m = "" + gcal.Month, d = "" + gcal.Day;
            string h = "" + gcal.Hour, min = "" + gcal.Minute;
            if (gcal.Month < 10)
            {
                m = "0" + m;
            }
            if (gcal.Day < 10)
            {
                d = "0" + d;
            }
            if (gcal.Hour < 10)
            {
                h = "0" + h;
            }
            if (gcal.Minute < 10)
            {
                min = "0" + min;
            }
            return "[" + h + ":" + min + "]" + d + " / " + m + " / " + gcal.Year;
        }


        #endregion

    }
}