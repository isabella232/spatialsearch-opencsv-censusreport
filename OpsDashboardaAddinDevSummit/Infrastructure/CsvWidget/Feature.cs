//Copyright 2014 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;

namespace DataHandler
{
  /// <summary>
  /// Represents an individual feature created based on a row on a .csv file
  /// </summary>
  public class Feature
  {
    private IDictionary<string, object> _attributes;
    public IDictionary<string, object> Attributes
    {
      get { return _attributes; }
      set { _attributes = value; } 
    }
    
    public Feature(IDictionary<string, object> Attributes)
    {
      this.Attributes = Attributes;
    }
  }
  
  /// <summary>
  /// Metadata about the attributes, including user's selected lat field, selected long field, and selected display field
  /// </summary>
  public class FieldHeadersInfo
  {
    private Visibility _fieldsVisibility;
    public Visibility FieldsVisibility
    {
      get { return _fieldsVisibility; }
      set
      {
        if (_fieldsVisibility != value)
        {
          _fieldsVisibility = value;
          OnPropertyChanged("FieldsVisibility");
        }
      }
    }

    private List<FieldInfo> _fields;
    public List<FieldInfo> Fields
    {
      get { return _fields; }
      set
      {
        if (_fields != value)
        {
          _fields = value;
          OnPropertyChanged("Fields");
        }
      }
    }

    private FieldInfo _selectedLatField;
    public FieldInfo SelectedLatField
    {
      get { return _selectedLatField; }
      set
      {
        if (_selectedLatField != value)
        {
          _selectedLatField = value;
          OnPropertyChanged("SelectedLatField");
        }
      }
    }

    private FieldInfo _selectedLongField;
    public FieldInfo SelectedLongField
    {
      get { return _selectedLongField; }
      set
      {
        if (_selectedLongField != value)
        {
          _selectedLongField = value;
          OnPropertyChanged("SelectedLongField");
        }
      }
    }

    private FieldInfo _selectedDisplayField;
    public FieldInfo SelectedDisplayField
    {
      get { return _selectedDisplayField; }
      set
      {
        if (_selectedDisplayField != value)
        {
          _selectedDisplayField = value;
          OnPropertyChanged("SelectedDisplayField");
        }
      }
    }

    public static FieldHeadersInfo CreateFieldHeaderinfo(string newCSVFilePath, string LatFieldName = "", string LongFieldName = "", string DisplayFieldName="")
    {
      Row row = new Row();
      Row headerFields = new Row();
      using (CsvParser reader = new CsvParser(newCSVFilePath))
      {
        //Read the header line
        if (!reader.ReadOneRow(row))
          return null;
        headerFields = row;

        //Set up the field info 
        List<FieldInfo> FieldInfos = new List<FieldInfo>();

        FieldInfo SelectedLatField = null;
        FieldInfo SelectedLongField = null;
        foreach (string fieldName in headerFields)
        {
          FieldInfo field;

          //If lat/long field has been set previously, use it
          if (fieldName == LatFieldName)
          {
            field = new FieldInfo(LatFieldName, true, false);
            SelectedLatField = field;
          }
          else if (fieldName == LongFieldName)
          {
            field = new FieldInfo(LongFieldName, false, true);
            SelectedLongField = field;
          }

          //Otherwise, try to find the fields that potentially contain location information 
          else if (string.Compare(fieldName.ToLower(), "y") == 0)
          {
            field = new FieldInfo(fieldName, true, false);
            if (SelectedLatField == null)
              SelectedLatField = field;
          }
          else if (fieldName.ToLower().Contains("lat"))
          {
            field = new FieldInfo(fieldName, true, false);
            if (SelectedLatField == null) 
              SelectedLatField = field;
          }
          else if (string.Compare(fieldName.ToLower(), "x") == 0)
          {
            field = new FieldInfo(fieldName, false, true);
            if (SelectedLongField == null)
              SelectedLongField = field;
          }
          else if (fieldName.ToLower().Contains("lon"))
          {
            field = new FieldInfo(fieldName, false, true);
            if (SelectedLongField == null)
              SelectedLongField = field;
          }
          else
            field = new FieldInfo(fieldName, false, false);

          FieldInfos.Add(field);
        }

        //Update properties to trigger UI change
        FieldHeadersInfo fieldHeadersInfo = new FieldHeadersInfo();
        fieldHeadersInfo.Fields = FieldInfos;
        fieldHeadersInfo.SelectedLatField = SelectedLatField == null ? fieldHeadersInfo.Fields.First() : SelectedLatField;
        fieldHeadersInfo.SelectedLongField = SelectedLongField == null ? fieldHeadersInfo.Fields.First() : SelectedLongField;
        fieldHeadersInfo.SelectedDisplayField = string.IsNullOrEmpty(DisplayFieldName) ? fieldHeadersInfo.Fields.First() : fieldHeadersInfo.Fields.First(f => f.FieldName == DisplayFieldName);

        fieldHeadersInfo.FieldsVisibility = Visibility.Visible;

        return fieldHeadersInfo;
      }
    }

    #region INotifyPropertyChanged
    void OnPropertyChanged(string prop)
    {
      if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }

  /// <summary>
  /// Metadata of a field, including the field's name, and whether it is a lat field or long field
  /// </summary>
  public class FieldInfo
  {
    public FieldInfo() { }

    public FieldInfo(string fieldName, bool isLat, bool isLong)
    {
      FieldName = fieldName;
      IsLat = isLat;
      IsLong = isLong;
    }

    public string FieldName { get; set; }
    public bool IsLat { get; set; }
    public bool IsLong { get; set; }
  }
}
