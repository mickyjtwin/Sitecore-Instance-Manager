using System;
using System.CodeDom;
using System.Xml;
using SIM.Instances;
using SIM.Pipelines.InstallModules;
using SIM.Products;

namespace SIM.Pipelines.Install.Modules
{
  class CreateSolrCollections : IPackageInstallActions
  {

    public void Execute(Instance instance, Product module)
    {
      XmlDocument config = instance.GetShowconfig();
      string url = config.SelectSingleNode("/sitecore/settings/setting[@name='ContentSearch.Solr.ServiceBaseAddress']").Attributes["value"].Value;

      XmlNodeList solrIndexes =
          config.SelectNodes(
              "/sitecore/contentSearch/configuration/indexes/index[@type='Sitecore.ContentSearch.SolrProvider.SolrSearchIndex, Sitecore.ContentSearch.SolrProvider']");
      foreach (var node in solrIndexes)
      {
        var element = node as XmlElement;
        if (element == null) continue;
        string id = element.Attributes["id"].InnerText;
        var coreElement = element.SelectSingleNode("param[@desc='core']") as XmlElement;
       
        if (coreElement == null) continue;

        string coreText = coreElement.InnerText.Replace("$(id)", id);

        Log.Info("Core found:"+coreText, this);

        var response = WebRequestHelper.RequestAndGetResponse(string.Format(
          "{0}/admin/collections?action=CREATE&name={1}&numShards=2", url, coreText));

         
      }


    }
  }
}
