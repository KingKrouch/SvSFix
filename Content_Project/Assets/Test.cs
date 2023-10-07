using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("hi there");
      // Displays the property values of the RegionInfo for "AU".
      RegionInfo myRI1 = new RegionInfo( "AU" );
      Debug.Log( "   Name:                         "+ myRI1.Name );
      Debug.Log( "   DisplayName:                  "+ myRI1.DisplayName );
      Debug.Log( "   EnglishName:                  "+ myRI1.EnglishName );
      Debug.Log( "   IsMetric:                     "+ myRI1.IsMetric );
      Debug.Log( "   ThreeLetterISORegionName:     "+ myRI1.ThreeLetterISORegionName );
      Debug.Log( "   ThreeLetterWindowsRegionName: "+ myRI1.ThreeLetterWindowsRegionName );
      Debug.Log( "   TwoLetterISORegionName:       "+ myRI1.TwoLetterISORegionName );
      Debug.Log( "   CurrencySymbol:               "+ myRI1.CurrencySymbol );
      Debug.Log( "   ISOCurrencySymbol:            "+ myRI1.ISOCurrencySymbol );
      Debug.Log("");
 
      // Compares the RegionInfo above with another RegionInfo created using CultureInfo.
      RegionInfo myRI2 = new RegionInfo( new CultureInfo("en-AU",false).LCID );
      if ( myRI1.Equals( myRI2 ) )
         Debug.Log( "The two RegionInfo instances are equal." );
      else
         Debug.Log( "The two RegionInfo instances are NOT equal." );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
