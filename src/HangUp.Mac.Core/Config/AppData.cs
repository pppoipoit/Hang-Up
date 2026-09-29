using System.Collections.Generic;

namespace HangUp.Mac.Core.Config
{
    public static class AppData
    {
        public static string GetJson()
        {
            return @"{
  ""apps"": [
    {
      ""name"": ""Adobe"",
      ""paths"": [""/Applications/Adobe*"", ""/Applications/Utilities/Adobe*"", ""~/Library/Application Support/Adobe""],
      ""domains"": [
        ""adobe.io"",
        ""adobestats.io"",
        ""adobegenuine.com"",
        ""prod.adobegenuine.com"",
        ""genuine.adobe.com"",
        ""cc-api-data.adobe.io"",
        ""ic.adobe.io"",
        ""lcs-cpc.adobe.io"",
        ""lcs-robs.adobe.io"",
        ""accounts.adobe.com"",
        ""adobelogin.com"",
        ""ims-na1.adobelogin.com"",
        ""ims-prod06.adobelogin.com"",
        ""auth.services.adobe.com"",
        ""adobeid-na1.services.adobe.com"",
        ""na1r.services.adobe.com"",
        ""lmlicenses.wip4.adobe.com"",
        ""lm.licenses.adobe.com"",
        ""activate.adobe.com"",
        ""practivate.adobe.com"",
        ""ereg.adobe.com"",
        ""wip3.adobe.com"",
        ""wip.adobe.com"",
        ""hl2rcv.adobe.com"",
        ""licenses.adobe.com"",
        ""license.adobe.com"",
        ""oobe.adobe.com"",
        ""sstats.adobe.com"",
        ""assets.adobedtm.com"",
        ""uds.adobe.com"",
        ""armmf.adobe.com"",
        ""crlog-crcn.adobe.com"",
        ""cc-api-cp.adobe.io"",
        ""cai-identity.adobe.io""
      ],
      ""icon"": ""Adobe.png"",
      ""gradientStart"": ""#3b82f6"",
      ""gradientEnd"": ""#06b6d4""
    },
    {
      ""name"": ""Autodesk"",
      ""paths"": [""/Applications/Autodesk*"", ""~/Library/Application Support/Autodesk""],
      ""domains"": [
        ""autodesk.com"",
        ""genuine.autodesk.com"",
        ""cur.autodesk.com"",
        ""app.core.collaboration.autodesk.com"",
        ""api.autodesk.com"",
        ""ase-cdn.autodesk.com"",
        ""stats.autodesk.com"",
        ""clic.autodesk.com"",
        ""identity.autodesk.com"",
        ""auth.autodesk.com"",
        ""accounts.autodesk.com"",
        ""developer.api.autodesk.com""
      ],
      ""icon"": ""Autodesk.png"",
      ""gradientStart"": ""#fb923c"",
      ""gradientEnd"": ""#f43f5e""
    },
    {
      ""name"": ""SolidWorks"",
      ""paths"": [""/Applications/SolidWorks*""],
      ""domains"": [
        ""solidworks.com"",
        ""ds.betonsim.com"",
        ""customerportal.solidworks.com""
      ],
      ""icon"": ""SolidWork.png"",
      ""gradientStart"": ""#14b8a6"",
      ""gradientEnd"": ""#3b82f6""
    },
    {
      ""name"": ""Corel"",
      ""paths"": [""/Applications/Corel*"", ""~/Library/Application Support/Corel""],
      ""domains"": [
        ""corel.com"",
        ""updates.corel.com"",
        ""iws.corel.com"",
        ""mc.corel.com"",
        ""apps.corel.com""
      ],
      ""icon"": ""Corel.png"",
      ""gradientStart"": ""#8b5cf6"",
      ""gradientEnd"": ""#ec4899""
    }
  ]
}";
        }
    }
}
