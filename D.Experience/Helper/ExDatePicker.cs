﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace D.Experience.Helper
{
    /// <summary>
    /// 只显示年月的DatePicker
    /// </summary>
    public class ExDatePicker
    {
        #region DisplayMode

        public static CalendarMode GetDisplayMode(DependencyObject obj)
        {
            return (CalendarMode)obj.GetValue(DisplayModeProperty);
        }

        public static void SetDisplayMode(DependencyObject obj, CalendarMode value)
        {
            obj.SetValue(DisplayModeProperty, value);
        }

        // Using a DependencyProperty as the backing store for DisplayMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.RegisterAttached("DisplayMode", typeof(CalendarMode), typeof(ExDatePicker), new PropertyMetadata(CalendarMode.Month, new PropertyChangedCallback(OnDisplayModeChanged)), new ValidateValueCallback(IsValidDisplayMode));
        private static bool IsValidDisplayMode(object value)
        {
            CalendarMode mode = (CalendarMode)value;

            return mode == CalendarMode.Year
                || mode == CalendarMode.Decade
                ||mode==CalendarMode.Month;
        }
        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var datePicker = (DatePicker)d;

            Application.Current.Dispatcher
                .BeginInvoke(DispatcherPriority.Loaded,
                             new Action<DatePicker, DependencyPropertyChangedEventArgs>(SetCalendarEventHandlers),
                             datePicker, e);
        }

        private static void SetCalendarEventHandlers(DatePicker datePicker, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;
            CalendarMode mode = (CalendarMode)e.NewValue;
            if (mode == CalendarMode.Year || mode == CalendarMode.Decade)
            {
                datePicker.CalendarOpened += DatePickerOnCalendarOpened;
                datePicker.CalendarClosed += DatePickerOnCalendarClosed;
            }
            else
            {
                datePicker.CalendarOpened -= DatePickerOnCalendarOpened;
                datePicker.CalendarClosed -= DatePickerOnCalendarClosed;
            }
        }

        private static void DatePickerOnCalendarOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            var calendar = GetDatePickerCalendar(sender);
            if (GetDisplayMode(sender as DatePicker) == CalendarMode.Decade)
            {
                calendar.DisplayMode = CalendarMode.Decade;
            }
            else
            {
                calendar.DisplayMode = CalendarMode.Year;
            }
            calendar.DisplayModeChanged += CalendarOnDisplayModeChanged;
        }

        private static void DatePickerOnCalendarClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var datePicker = (DatePicker)sender;
            var calendar = GetDatePickerCalendar(sender);
            datePicker.SelectedDate = calendar.SelectedDate;

            calendar.DisplayModeChanged -= CalendarOnDisplayModeChanged;
        }

        private static void CalendarOnDisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            var calendar = (Calendar)sender;

            var datePicker = GetCalendarsDatePicker(calendar);
            bool mode = (GetDisplayMode(datePicker) == CalendarMode.Decade && calendar.DisplayMode != CalendarMode.Year) ||
                (GetDisplayMode(datePicker) == CalendarMode.Year && calendar.DisplayMode != CalendarMode.Month);
            if (mode)
                return;

            calendar.SelectedDate = GetSelectedCalendarDate(calendar.DisplayDate);


            datePicker.IsDropDownOpen = false;
        }

        private static Calendar GetDatePickerCalendar(object sender)
        {
            var datePicker = (DatePicker)sender;
            var popup = (Popup)datePicker.Template.FindName("PART_Popup", datePicker);
            return ((Calendar)popup.Child);
        }

        private static DatePicker GetCalendarsDatePicker(FrameworkElement child)
        {
            var parent = (FrameworkElement)child.Parent;
            if (parent.Name == "PART_Root")
                return (DatePicker)parent.TemplatedParent;
            return GetCalendarsDatePicker(parent);
        }

        private static DateTime? GetSelectedCalendarDate(DateTime? selectedDate)
        {
            if (!selectedDate.HasValue)
                return null;
            return new DateTime(selectedDate.Value.Year, selectedDate.Value.Month, 1);
        }
        #endregion

        #region DateFormat

        /// <summary>
        /// 显示年月的日期格式
        /// </summary>
        public static readonly DependencyProperty DateFormatProperty =
            DependencyProperty.RegisterAttached("DateFormat", typeof(string), typeof(ExDatePicker), new PropertyMetadata(OnDateFormatChanged));

        public static string GetDateFormat(DependencyObject dobj)
        {
            return (string)dobj.GetValue(DateFormatProperty);
        }

        public static void SetDateFormat(DependencyObject dobj, string value)
        {
            dobj.SetValue(DateFormatProperty, value);
        }

        private static void OnDateFormatChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            var datePicker = (DatePicker)dobj;

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded, new Action<DatePicker>(ApplyDateFormat), datePicker);
        }

        private static void ApplyDateFormat(DatePicker datePicker)
        {
            var binding = new Binding("SelectedDate")
            {
                RelativeSource = new RelativeSource { AncestorType = typeof(DatePicker) },
                Converter = new DatePickerDateTimeConverter(),
                ConverterParameter = new Tuple<DatePicker, string>(datePicker, GetDateFormat(datePicker))
            };
            var textBox = GetTemplateTextBox(datePicker);
            textBox.SetBinding(TextBox.TextProperty, binding);

            textBox.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
            textBox.PreviewKeyDown += TextBoxOnPreviewKeyDown;
            //在公用控件的基础上添加此事件，防止失去焦点的时候触发多次selecteddatecahnged事件
            textBox.LostKeyboardFocus -= textBox_LostKeyboardFocus;
            textBox.LostKeyboardFocus += textBox_LostKeyboardFocus;

            datePicker.CalendarOpened -= DatePickerOnCalendarOpened2;
            datePicker.CalendarOpened += DatePickerOnCalendarOpened2;
        }

        private static void textBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;

            var textBox = (TextBox)sender;
            var datePicker = (DatePicker)textBox.TemplatedParent;
            var dateStr = textBox.Text;
            var formatStr = GetDateFormat(datePicker);
            datePicker.SelectedDate = DatePickerDateTimeConverter.StringToDateTime(datePicker, formatStr, dateStr);
        }
        private static TextBox GetTemplateTextBox(Control control)
        {
            control.ApplyTemplate();
            return (TextBox)control.Template.FindName("PART_TextBox", control);
        }

        private static void TextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            /* DatePicker subscribes to its TextBox's KeyDown event to set its SelectedDate if Key.Return was
             * pressed. When this happens its text will be the result of its internal date parsing until it
             * loses focus or another date is selected. A workaround is to stop the KeyDown event bubbling up
             * and handling setting the DatePicker.SelectedDate. */

            e.Handled = true;

            var textBox = (TextBox)sender;
            var datePicker = (DatePicker)textBox.TemplatedParent;
            var dateStr = textBox.Text;
            var formatStr = GetDateFormat(datePicker);
            datePicker.SelectedDate = DatePickerDateTimeConverter.StringToDateTime(datePicker, formatStr, dateStr);
        }

        private static void DatePickerOnCalendarOpened2(object sender, RoutedEventArgs e)
        {
            /* When DatePicker's TextBox is not focused and its Calendar is opened by clicking its calendar button
             * its text will be the result of its internal date parsing until its TextBox is focused and another
             * date is selected. A workaround is to set this string when it is opened. */

            var datePicker = (DatePicker)sender;
            var textBox = GetTemplateTextBox(datePicker);
            var formatStr = GetDateFormat(datePicker);
            textBox.Text = DatePickerDateTimeConverter.DateTimeToString(formatStr, datePicker.SelectedDate);
        }

        private class DatePickerDateTimeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var formatStr = ((Tuple<DatePicker, string>)parameter).Item2;
                var selectedDate = (DateTime?)value;
                return DateTimeToString(formatStr, selectedDate);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var tupleParam = ((Tuple<DatePicker, string>)parameter);
                var dateStr = (string)value;
                return StringToDateTime(tupleParam.Item1, tupleParam.Item2, dateStr);
            }

            public static string DateTimeToString(string formatStr, DateTime? selectedDate)
            {
                return selectedDate.HasValue ? selectedDate.Value.ToString(formatStr) : null;
            }

            public static DateTime? StringToDateTime(DatePicker datePicker, string formatStr, string dateStr)
            {
                DateTime date;
                var canParse = DateTime.TryParseExact(dateStr, formatStr, System.Globalization.CultureInfo.CurrentCulture,
                                                      System.Globalization.DateTimeStyles.None, out date);

                if (!canParse)
                    canParse = DateTime.TryParse(dateStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out date);

                return canParse ? date : datePicker.SelectedDate;
            }
        }

        #endregion
    }
}
