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

using ESRI.ArcGIS.Client.Tasks;

namespace SelectByLocation
{
  public class SearchUnit
  {
    public string String
    {
      get
      {
        if (Unit == LinearUnit.Centimeter)
          return "Centimeter";
        else if (Unit == LinearUnit.Foot)
          return "Foot";
        else if (Unit == LinearUnit.InternationalInch)
          return "Inch";
        else if (Unit == LinearUnit.Kilometer)
          return "Kilometer";
        else if (Unit == LinearUnit.Meter)
          return "Meter";
        else if (Unit == LinearUnit.SurveyMile)
          return "Mile";
        else if (Unit == LinearUnit.InternationalYard)
          return "Yard";
        else
          return "";
      }
    }

    public LinearUnit Unit { get; private set; }

    public SearchUnit(LinearUnit unit)
    {
      Unit = unit;
    }
  }
}
