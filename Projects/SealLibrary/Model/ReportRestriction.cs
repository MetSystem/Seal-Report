﻿//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Seal.Helpers;
using Seal.Converter;
using Seal.Forms;

namespace Seal.Model
{


    [ClassResource(BaseName = "DynamicTypeDescriptorApp.Properties.Resources", KeyPrefix = "ReportRestriction_")]
    public class ReportRestriction : ReportElement
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Prompt").SetIsBrowsable(true);
                GetProperty("Required").SetIsBrowsable(true);
                GetProperty("Operator").SetIsBrowsable(true);
                GetProperty("DisplayNameEl").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("FormatRe").SetIsBrowsable(!IsEnum);
                GetProperty("TypeRe").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("OperatorLabel").SetIsBrowsable(true);
                GetProperty("EnumGUIDRE").SetIsBrowsable(true);
                GetProperty("UseAsParameter").SetIsBrowsable(true);

                //Conditional
                if (IsEnum)
                {
                    GetProperty("EnumValue").SetIsBrowsable(true);
                }
                else if (IsDateTime)
                {
                    GetProperty("Date1").SetIsBrowsable(true);
                    GetProperty("Date2").SetIsBrowsable(true);
                    GetProperty("Date3").SetIsBrowsable(true);
                    GetProperty("Date4").SetIsBrowsable(true);
                    GetProperty("Date1Keyword").SetIsBrowsable(true);
                    GetProperty("Date2Keyword").SetIsBrowsable(true);
                    GetProperty("Date3Keyword").SetIsBrowsable(true);
                    GetProperty("Date4Keyword").SetIsBrowsable(true);
                }
                else
                {
                    GetProperty("Value1").SetIsBrowsable(true);
                    GetProperty("Value2").SetIsBrowsable(true);
                    GetProperty("Value3").SetIsBrowsable(true);
                    GetProperty("Value4").SetIsBrowsable(true);
                }

                if (!IsEnum)
                {
                    GetProperty("NumericStandardFormatRe").SetIsBrowsable(IsNumeric);
                    GetProperty("DateTimeStandardFormatRe").SetIsBrowsable(IsDateTime);
                }

                //Readonly
                foreach (var property in Properties) property.SetIsReadOnly(false);

                GetProperty("FormatRe").SetIsReadOnly((IsNumeric && NumericStandardFormat != NumericStandardFormat.Custom) || (IsDateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom));
                if (_operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty)
                {
                    GetProperty("Value1").SetIsReadOnly(true);
                    GetProperty("Date1").SetIsReadOnly(true);
                    GetProperty("Date1Keyword").SetIsReadOnly(true);
                    GetProperty("EnumValue").SetIsReadOnly(true);
                }

                if (IsGreaterSmallerOperator || _operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty || _operator == Operator.ValueOnly)
                {
                    GetProperty("Value2").SetIsReadOnly(true);
                    GetProperty("Date2").SetIsReadOnly(true);
                    GetProperty("Date2Keyword").SetIsReadOnly(true);
                }

                if (_operator == Operator.Between || _operator == Operator.NotBetween || IsGreaterSmallerOperator || _operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty || _operator == Operator.ValueOnly)
                {
                    GetProperty("Value3").SetIsReadOnly(true);
                    GetProperty("Date3").SetIsReadOnly(true);
                    GetProperty("Date3Keyword").SetIsReadOnly(true);

                    GetProperty("Value4").SetIsReadOnly(true);
                    GetProperty("Date4").SetIsReadOnly(true);
                    GetProperty("Date4Keyword").SetIsReadOnly(true);
                }

                GetProperty("UseAsParameter").SetIsReadOnly(_operator != Operator.ValueOnly);
                GetProperty("Required").SetIsReadOnly(_prompt == PromptType.None);

                //Aggregate restriction
                if (PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate) GetProperty("AggregateFunction").SetIsBrowsable(true);

                if (!GetProperty("Date1Keyword").IsReadOnly) GetProperty("Date1").SetIsReadOnly(HasDateKeyword(Date1Keyword));
                if (!GetProperty("Date2Keyword").IsReadOnly) GetProperty("Date2").SetIsReadOnly(HasDateKeyword(Date2Keyword));
                if (!GetProperty("Date3Keyword").IsReadOnly) GetProperty("Date3").SetIsReadOnly(HasDateKeyword(Date3Keyword));
                if (!GetProperty("Date4Keyword").IsReadOnly) GetProperty("Date4").SetIsReadOnly(HasDateKeyword(Date4Keyword));

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public const char kStartRestrictionChar = '[';
        public const char kStopRestrictionChar = ']';

        public static ReportRestriction CreateReportRestriction()
        {
            return new ReportRestriction() { GUID = Guid.NewGuid().ToString(), _type = ColumnType.Default, _numericStandardFormat = NumericStandardFormat.Default, _datetimeStandardFormat = DateTimeStandardFormat.Default };
        }

        private PromptType _prompt = PromptType.None;
        [CategoryAttribute("Definition"), DisplayName("Prompt restriction"), Description("Define if the value of the restriction is prompted to the user when the report is executed."), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PromptType Prompt
        {
            get { return _prompt; }
            set { _prompt = value; }
        }

        private bool _required = false;
        [CategoryAttribute("Definition"), DisplayName("Is required"), Description("If true and the restriction is prompted, a value is required to execute the report."), Id(4, 1)]
        public bool Required
        {
            get { return _required; }
            set { _required = value; }
        }

        [Category("Advanced"), DisplayName("Custom SQL"), Description("If not empty, overwrite the default SQL used for the restriction in the WHERE clause."), Id(1, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public new string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }

        [Category("Advanced"), DisplayName("Data Type"), Description("Data type of the restriction."), Id(2, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnType TypeRe
        {
            get { return _type; }
            set
            {
                Type = value;
                UpdateEditorAttributes();
            }
        }

        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(3, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public DateTimeStandardFormat DateTimeStandardFormatRe
        {
            get { return _datetimeStandardFormat; }
            set
            {
                _datetimeStandardFormat = value;
                if (_dctd != null)
                {
                    SetStandardFormat();
                    UpdateEditorAttributes();
                }
            }
        }

        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(3, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public NumericStandardFormat NumericStandardFormatRe
        {
            get { return _numericStandardFormat; }
            set
            {
                _numericStandardFormat = value;
                if (_dctd != null)
                {
                    SetStandardFormat();
                    UpdateEditorAttributes();
                }
            }
        }

        [Category("Advanced"), DisplayName("Custom Format"), Description("If not empty, specify the format of the restriction display values (.Net Format Strings)."), Id(4, 3)]
        [TypeConverter(typeof(CustomFormatConverter))]
        public string FormatRe
        {
            get {
                SetDefaultFormat();
                return _format; 
            }
            set { 
                _format = value;
            }
        }

        private string _operatorLabel;
        [Category("Advanced"), DisplayName("Operator Label"), Description("If not empty, overwrite the operator display text."), Id(5, 3)]
        public string OperatorLabel
        {
            get { return _operatorLabel; }
            set { _operatorLabel = value; }
        }


        [Category("Advanced"), DisplayName("Custom Enumerated List"), Description("If defined, the restriction values are selected using the enumerated list."), Id(6, 3)]
        [TypeConverter(typeof(MetaEnumConverter))]
        public string EnumGUIDRE
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }

        private bool _useAsParameter;
        [Category("Advanced"), DisplayName("Use as parameter"), Description("If true and the operator is set to Value Only, the restriction is replaced by '(1=1') and has no impact on the SQL generated. The value can then be used in scripts."), Id(7, 3)]
        public bool UseAsParameter
        {
            get { return _useAsParameter; }
            set { _useAsParameter = value; }
        }

        Operator _operator = Operator.Equal;
        [TypeConverter(typeof(RestrictionOperatorConverter))]
        [Category("Definition"), DisplayName("Operator"), Description("The Operator used for the restriction. If Value Only is selected, the restriction is replaced by the value only (with no column name and operator)."), Id(2, 1)]
        public Operator Operator
        {
            get { return _operator; }
            set
            {
                _operator = value;
                UpdateEditorAttributes();
            }
        }

        [XmlIgnore]
        public MetaEnum EnumRE
        {
            get
            {
                if (Enum != null) return Enum;
                return MetaColumn.Enum;
            }
        }

        [XmlIgnore]
        public bool IsEnumRE
        {
            get
            {
                if (Enum != null) return true;
                return IsEnum;
            }
        }

        [XmlIgnore]
        public bool HasOperator
        {
            get
            {
                return !(Operator == Operator.ValueOnly && string.IsNullOrEmpty(OperatorLabel));
            }
        }

        public string GetOperatorLabel(Operator op)
        {
            if (Operator == Operator.ValueOnly) return OperatorLabel;
            return Model.Report.Translate(Helper.GetEnumDescription(typeof(Operator), op));
        }

        void CheckInputValue(string value)
        {
            if (Source == null) return;
            if (IsNumeric && !string.IsNullOrEmpty(value))
            {
                Double result;
                if (!Double.TryParse(value, out result)) throw new Exception("Invalid numeric value: " + value);
            }
        }

        public void SetNavigationValue(string val)
        {
            if (IsEnum) EnumValues.Add(val);
            else if (IsDateTime) Date1 = DateTime.FromOADate(double.Parse(val, CultureInfo.InvariantCulture));
            else Value1 = val;
        }

        [XmlIgnore]
        public List<Operator> AllowedOperators
        {
            get
            {
                List<Operator> result = new List<Operator>();
                result.Add(Operator.Equal);
                result.Add(Operator.NotEqual);
                if (IsText && !IsEnum)
                {
                    result.Add(Operator.Contains);
                    result.Add(Operator.NotContains);
                    result.Add(Operator.StartsWith);
                    result.Add(Operator.EndsWith);
                    result.Add(Operator.IsEmpty);
                    result.Add(Operator.IsNotEmpty);
                }
                if (!IsEnum)
                {
                    result.Add(Operator.Between);
                    result.Add(Operator.NotBetween);
                    result.Add(Operator.Smaller);
                    result.Add(Operator.SmallerEqual);
                    result.Add(Operator.Greater);
                    result.Add(Operator.GreaterEqual);
                }
                result.Add(Operator.IsNull);
                result.Add(Operator.IsNotNull);
                result.Add(Operator.ValueOnly);
                return result;
            }
        }

        [XmlIgnore]
        public List<Operator> AllowedDisplayOperators
        {
            get
            {
                if (Operator == Seal.Model.Operator.ValueOnly) return AllowedOperators.Where(i => i == Seal.Model.Operator.ValueOnly).ToList();
                return AllowedOperators.Where(i => i != Seal.Model.Operator.ValueOnly).ToList();

            }
        }


        string _value1;
        [Category("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction."), Id(1, 2)]
        public string Value1
        {
            get { return _value1; }
            set
            {
                CheckInputValue(value);
                _value1 = value;
            }
        }

        string _value2;
        [Category("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 2)]
        public string Value2
        {
            get { return _value2; }
            set
            {
                CheckInputValue(value);
                _value2 = value;
            }
        }

        string _value3;
        [Category("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 2)]
        public string Value3
        {
            get { return _value3; }
            set
            {
                CheckInputValue(value);
                _value3 = value;
            }
        }

        string _value4;
        [Category("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 2)]
        public string Value4
        {
            get { return _value4; }
            set
            {
                CheckInputValue(value);
                _value4 = value;
            }
        }

        public double? DoubleValue
        {
            get
            {
                if (!HasValue1) return null;
                double result;
                if (double.TryParse(Value1.ToString(), out result)) return result;
                return null;
            }
        }


        List<string> _enumValues = new List<string>();
        public List<string> EnumValues
        {
            get { return _enumValues; }
            set { _enumValues = value; }
        }

        [Category("Restriction Values"), DisplayName("Value"), Description("Value used for the restriction."), Id(1, 2)]
        [Editor(typeof(RestrictionEnumValuesEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public string EnumValue
        {
            get { return "<Click to edit values>"; }
            set { } //keep set for modification handler
        }


        [CategoryAttribute("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction."), Id(1, 2)]
        public DateTime Date1 { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 2)]
        public DateTime Date2 { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 2)]
        public DateTime Date3 { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 2)]
        public DateTime Date4 { get; set; }

        [Category("Restriction Values"), DisplayName("Value 1 Keyword"), Description("Date keyword can be used to specify relative date and time for the restriction value."), Id(2, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date1Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 2 Keyword"), Description("Date keyword can be used to specify relative date and time for the restriction value."), Id(4, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date2Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 3 Keyword"), Description("Date keyword can be used to specify relative date and time for the restriction value."), Id(6, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date3Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 4 Keyword"), Description("Date keyword can be used to specify relative date and time for the restriction value."), Id(8, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date4Keyword { get; set; }



        public bool HasValue
        {
            get
            {
                return Operator == Operator.IsNull
                    || Operator == Operator.IsNotNull
                    || Operator == Operator.IsEmpty
                    || Operator == Operator.IsNotEmpty
                    || (IsEnum && EnumValues.Count > 0)
                    || HasValue1
                    || (HasValue2 && !IsGreaterSmallerOperator)
                    || (HasValue3 && !IsGreaterSmallerOperator && !IsBetweenOperator)
                    || (HasValue4 && !IsGreaterSmallerOperator && !IsBetweenOperator);
            }
        }

        public bool HasValue1
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date1Keyword) || Date1 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value1))
                    );
            }
        }

        public bool HasValue2
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date2Keyword) || Date2 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value2))
                    );
            }
        }

        public bool HasValue3
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date3Keyword) || Date3 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value3))
                    );
            }
        }

        public bool HasValue4
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date4Keyword) || Date4 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value4))
                    );
            }
        }

        static public bool HasDateKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;

            return
                keyword.StartsWith(DateRestrictionKeyword.Now.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.Today.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisWeek.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisMonth.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisYear.ToString());
        }

        double GetGap(string datekeyword, string keyword)
        {
            string val = datekeyword.Replace(keyword, "");
            double result = 0;
            double.TryParse(val, out result);
            return result;
        }

        DateTime GetFinalDate(string dateKeyword, DateTime date)
        {
            DateTime result = date;
            if (!string.IsNullOrEmpty(dateKeyword))
            {
                if (dateKeyword.StartsWith(DateRestrictionKeyword.Now.ToString())) result = DateTime.Now;
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.Today.ToString()))
                {
                    result = DateTime.Today.AddDays(GetGap(dateKeyword, (DateRestrictionKeyword.Today.ToString())));
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisWeek.ToString()))
                {
                    //First monday of the week...
                    result = DateTime.Today.AddDays(1 - (int)DateTime.Today.DayOfWeek).AddDays(7 * GetGap(dateKeyword, (DateRestrictionKeyword.ThisWeek.ToString())));
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisMonth.ToString()))
                {
                    result = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(Convert.ToInt32(GetGap(dateKeyword, (DateRestrictionKeyword.ThisMonth.ToString()))));
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisYear.ToString()))
                {
                    result = new DateTime(DateTime.Today.Year, 1, 1).AddYears(Convert.ToInt32(GetGap(dateKeyword, (DateRestrictionKeyword.ThisYear.ToString()))));
                }
            }
            else if (date == DateTime.MinValue) result = DateTime.Now;
            return result;
        }

        public DateTime FinalDate1
        {
            get
            {
                return GetFinalDate(Date1Keyword, Date1);
            }
        }

        public DateTime FinalDate2
        {
            get
            {
                return GetFinalDate(Date2Keyword, Date2);
            }
        }

        public DateTime FinalDate3
        {
            get
            {
                return GetFinalDate(Date3Keyword, Date3);
            }
        }

        public DateTime FinalDate4
        {
            get
            {
                return GetFinalDate(Date4Keyword, Date4);
            }
        }

        string EnumDisplayValue
        {
            get
            {
                string result = "";
                if (IsEnum)
                {
                    foreach (string enumValue in EnumValues)
                    {
                        Helper.AddValue(ref result, Model.Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, Model.Report.EnumDisplayValue(EnumRE, enumValue, true));
                    }
                }
                return result;
            }
        }

        string GetDisplayValue(string value, DateTime date)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "0";
                else result = ElementDisplayValue(double.Parse(value, NumberStyles.Any));
            }
            else if (IsDateTime)
            {
                if (date == DateTime.MinValue) date = DateTime.Now;
                result = "'" + ElementDisplayValue(date)  + "'";
            }
            else
            {
                if (string.IsNullOrEmpty(value)) value = "";
                result = "'" + ElementDisplayValue(value) + "'";
            }
            return result;
        }

        public string GeNavigationDisplayValue()
        {
            var result = IsEnum ? EnumDisplayValue : GetDisplayValue(Value1, Date1);
            if (result.Length > 2 && result[0] == '\'' && result[result.Length-1] == '\'') result = result.Substring(1, result.Length - 2);
            return result;
        }

        string GetDisplayRestriction(string value, string dateKeyword, DateTime date)
        {
            string result = "";
            if (IsDateTime)
            {
                if (!string.IsNullOrEmpty(dateKeyword)) result = Helper.QuoteSingle(dateKeyword);
                else result = GetDisplayValue(null, date);
            }
            else
            {
                result = GetDisplayValue(value, DateTime.MinValue);
            }
            return result;
        }

        public string GetEnumDisplayValue(string id)
        {
            return Model.Report.EnumDisplayValue(EnumRE, id, true);
        }

        public bool IsGreaterSmallerOperator
        {
            get { return _operator == Operator.Greater || _operator == Operator.GreaterEqual || _operator == Operator.Smaller || _operator == Operator.SmallerEqual; }
        }

        public bool IsContainOperator
        {
            get { return _operator == Operator.Contains || _operator == Operator.StartsWith || _operator == Operator.EndsWith || _operator == Operator.NotContains; }
        }

        public bool IsBetweenOperator
        {
            get { return _operator == Operator.Between || _operator == Operator.NotBetween; }
        }

        string EnumSQLValue
        {
            get
            {

                string result = "";
                if (IsEnum)
                {
                    if (EnumValues.Count == 0) result = (MetaColumn.Type == ColumnType.Numeric ? "0" : "''");
                    foreach (string enumValue in EnumValues)
                    {
                        Helper.AddValue(ref result, ",", MetaColumn.Type == ColumnType.Numeric ? enumValue : Helper.QuoteSingle(enumValue));
                    }
                }
                return result;
            }
        }

        string GetSQLValue(string value, DateTime date, Operator op)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "0";
                else result = Double.Parse(value).ToString(CultureInfo.InvariantCulture.NumberFormat); ;
            }
            else if (IsDateTime)
            {
                if (date == DateTime.MinValue) date = DateTime.Now;
                if (Model.Connection.DatabaseType == DatabaseType.MSAccess || Model.Connection.DatabaseType == DatabaseType.MSExcel)
                {
                    //Serial
                    result = Double.Parse(date.ToOADate().ToString()).ToString(CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    //Ansi Format
                    result = Helper.QuoteSingle(date.ToString(Model.Connection.DateTimeFormat));
                }
            }
            else
            {
                string value2 = value;
                if (string.IsNullOrEmpty(value)) value2 = "";
                if (op == Operator.Contains || op == Operator.NotContains) value2 = string.Format("%{0}%", value);
                else if (op == Operator.StartsWith) value2 = string.Format("{0}%", value);
                else if (op == Operator.EndsWith) value2 = string.Format("%{0}", value);
                result = Helper.QuoteSingle(value2);
                if (TypeEl == ColumnType.UnicodeText)
                {
                    if (Model.Connection.DatabaseType == DatabaseType.Oracle)
                    {
                        //For Oracle, we convert the unicode char using UNISTR
                        result = "";
                        for (int i = 0; i < value2.Length; i++)
                        {
                            string unicode = BitConverter.ToString(Encoding.Unicode.GetBytes(value2[i].ToString())).Replace("-", "");
                            result += "\\" + unicode.Substring(2, 2) + unicode.Substring(0, 2);
                        }
                        result = "UNISTR(" + Helper.QuoteSingle(result) + ")";
                    }
                    else if (Model.Connection.DatabaseType == DatabaseType.MSSQLServer)
                    {
                        result = "N" + result;
                    }
                }
            }
            return result;
        }

        void addEqualOperator(ref string displayText, ref string displayRestriction, ref string sqlText, string value, DateTime finalDate, string dateKeyword, DateTime date)
        {
            Helper.AddValue(ref displayText, Model.Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayValue(value, finalDate));
            Helper.AddValue(ref displayRestriction, Model.Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayRestriction(value, dateKeyword, date));
            Helper.AddValue(ref sqlText, ",", GetSQLValue(value, finalDate, _operator));
        }

        void addContainOperator(ref string displayText, ref string displayRestriction, ref string sqlText, string value, DateTime finalDate, string sqlOperator, string dateKeyword, DateTime date)
        {
            string separator = (_operator == Operator.NotContains ? " AND " : " OR ");
            Helper.AddValue(ref displayText, Model.Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayValue(value, finalDate));
            Helper.AddValue(ref displayRestriction, Model.Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayRestriction(value, dateKeyword, date));
            Helper.AddValue(ref sqlText, separator, string.Format("{0} {1}{2}", SQLColumn, sqlOperator, GetSQLValue(value, finalDate, _operator)));
        }



        void BuildTexts()
        {
            string displayLabel = DisplayNameElTranslated;

            if (_operator == Operator.ValueOnly)
            {
                if (IsEnum)
                {
                    _SQLText = UseAsParameter ? "(1=1)" : string.Format("({0})", (HasValue ? EnumSQLValue : "NULL"));
                    _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue ? EnumDisplayValue : "?");
                    _displayRestriction = _displayText;
                }
                else
                {
                    _SQLText = UseAsParameter ? "(1=1)" : (HasValue1 ? GetSQLValue(Value1, FinalDate1, _operator) : "NULL");
                    _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue1 ? GetDisplayValue(Value1, FinalDate1) : "?");
                    _displayRestriction = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue1 ? GetDisplayRestriction(Value1, Date1Keyword, Date1) : "?");
                }
                return;
            }

            _SQLText = "";

            string operatorLabel = Helper.GetEnumDescription(typeof(Operator), _operator);
            if (Model != null && Model.Report != null) operatorLabel = Model.Report.Translate(operatorLabel);
            _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? operatorLabel : OperatorLabel);
            _displayRestriction = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? operatorLabel : OperatorLabel);

            if (!HasValue)
            {
                _SQLText = "(1=1)";
                _displayText += " ?";
                _displayRestriction += " ?";
                return;
            }

            string sqlOperator = Helper.GetEnumDescription(typeof(Operator), _operator);
            if (_operator == Operator.IsNull || _operator == Operator.IsNotNull)
            {
                //Not or Not Null
                _SQLText += SQLColumn + " " + sqlOperator;
            }
            else if (_operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty)
            {
                string op = _operator == Operator.IsEmpty ? " = " : " <> ";
                //add a space to make it work with Oracle...
                string val = (Model != null && Model.Connection != null && Model.Connection.DatabaseType == DatabaseType.Oracle) ? "' '" : "''";
                _SQLText += SQLColumn + op + val;
            }
            else
            {
                //Other cases
                if (IsContainOperator)
                {
                    _SQLText = "(";
                    sqlOperator = (_operator == Operator.NotContains ? "NOT " : "") + "LIKE ";
                }
                else if (_operator == Operator.Equal || _operator == Operator.NotEqual)
                {
                    sqlOperator = (_operator == Operator.Equal ? "IN (" : "NOT IN (");
                }


                if (_operator == Operator.Between || _operator == Operator.NotBetween)
                {
                    _displayText += " " + GetDisplayValue(Value1, FinalDate1);
                    _displayRestriction += " " + GetDisplayRestriction(Value1, Date1Keyword, Date1);

                    _displayText += " " + Model.Report.Translate("AND") + " " + GetDisplayValue(Value2, FinalDate2);
                    _displayRestriction += " " + Model.Report.Translate("AND") + " " + GetDisplayRestriction(Value2, Date2Keyword, Date2);
                    if (Model.Source.IsNoSQL)
                    {
                        //Between is not supported for NoSQL
                        _SQLText += "(" + SQLColumn + ">=" + GetSQLValue(Value1, FinalDate1, _operator);
                        _SQLText += " AND " + SQLColumn + "<=" + GetSQLValue(Value2, FinalDate2, _operator) + ")";
                    }
                    else
                    {
                        _SQLText += "(" + SQLColumn + " " + sqlOperator + " ";
                        _SQLText += GetSQLValue(Value1, FinalDate1, _operator);
                        _SQLText += " AND " + GetSQLValue(Value2, FinalDate2, _operator) + ")";
                    }
                }
                else if (_operator == Operator.Equal || _operator == Operator.NotEqual)
                {
                    _SQLText += SQLColumn + " " + sqlOperator;
                    string displayText = "", displayRestriction = "", sqlText = "";
                    if (IsEnum)
                    {
                        displayText = EnumDisplayValue;
                        displayRestriction = displayText;
                        sqlText = EnumSQLValue;
                    }
                    else
                    {
                        if (HasValue1 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value1, FinalDate1, Date1Keyword, Date1);
                        if (HasValue2 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value2, FinalDate2, Date2Keyword, Date2);
                        if (HasValue3 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value3, FinalDate3, Date3Keyword, Date3);
                        if (HasValue4 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value4, FinalDate4, Date4Keyword, Date4);
                    }
                    _displayText += " " + displayText;
                    _displayRestriction += " " + displayRestriction;
                    _SQLText += sqlText + ")";
                }
                else if (IsContainOperator)
                {
                    string displayText = "", displayRestriction = "", sqlText = "";
                    if (HasValue1) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value1, FinalDate1, sqlOperator, Date1Keyword, Date1);
                    if (HasValue2) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value2, FinalDate2, sqlOperator, Date2Keyword, Date2);
                    if (HasValue3) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value3, FinalDate3, sqlOperator, Date3Keyword, Date3);
                    if (HasValue4) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value4, FinalDate4, sqlOperator, Date4Keyword, Date4);
                    _displayText += " " + displayText;
                    _displayRestriction += " " + displayRestriction;
                    _SQLText += sqlText + ")";
                }
                else
                {
                    if (IsGreaterSmallerOperator)
                    {
                        _SQLText += " ";
                    }
                    _SQLText += SQLColumn + " " + sqlOperator;
                    _displayText += " " + GetDisplayValue(Value1, FinalDate1);
                    _displayRestriction += " " + GetDisplayRestriction(Value1, Date1Keyword, Date1);
                    _SQLText += GetSQLValue(Value1, FinalDate1, _operator);
                }
            }
        }

        [XmlIgnore]
        string _displayRestriction;
        public string DisplayRestriction
        {
            get
            {
                BuildTexts();
                return _displayRestriction;
            }
        }

        [XmlIgnore]
        public string DisplayRestrictionForEditor
        {
            get
            {
                BuildTexts();
                return DisplayRestriction.Replace("[", "{").Replace("]","}");
            }
        }


        [XmlIgnore]
        string _displayText;
        public string DisplayText
        {
            get
            {
                BuildTexts();
                return _displayText;
            }
        }

        [XmlIgnore]
        string _SQLText;
        public string SQLText
        {
            get
            {
                BuildTexts();
                return _SQLText;
            }
        }

        [XmlIgnore]
        public string OperatorHtmlId
        {
            get
            {
                return HtmlIndex + "_Operator";
            }
        }

        [XmlIgnore]
        public string ValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Value";
            }
        }

        [XmlIgnore]
        public string OptionValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Option_Value";
            }
        }

        [XmlIgnore]
        public string OptionHtmlId
        {
            get
            {
                return HtmlIndex + "_Option";
            }
        }

        string _htmlIndex;
        [XmlIgnore]
        public string HtmlIndex
        {
            get { return _htmlIndex; }
            set { _htmlIndex = value; }
        }

        string GetHtmlValue(string value, string keyword, DateTime date)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "";
                else result = ElementDisplayValue(value);
            }
            else if (IsDateTime)
            {
                if (HasDateKeyword(keyword))
                {
                    result = keyword;
                }
                else if (date == DateTime.MinValue && !HasDateKeyword(keyword)) result = "";
                else
                {
                    date = GetFinalDate(keyword, date);
                    //for date, format should be synchro with the date picker, whîch should use short date
                    result = ((IFormattable)date).ToString(Model.Report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern, Model.Report.ExecutionView.CultureInfo);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(value)) value = "";
                result = ElementDisplayValue(value);
            }
            return result;
        }

        [XmlIgnore]
        public string Value1Html
        {
            get
            {
                return GetHtmlValue(Value1, Date1Keyword, Date1);
            }
        }

        [XmlIgnore]
        public string Value2Html
        {
            get
            {
                return GetHtmlValue(Value2, Date2Keyword, Date2);
            }
        }

        [XmlIgnore]
        public string Value3Html
        {
            get
            {
                return GetHtmlValue(Value3, Date3Keyword, Date3);
            }
        }

        [XmlIgnore]
        public string Value4Html
        {
            get
            {
                return GetHtmlValue(Value4, Date4Keyword, Date4);
            }
        }

    }
}
