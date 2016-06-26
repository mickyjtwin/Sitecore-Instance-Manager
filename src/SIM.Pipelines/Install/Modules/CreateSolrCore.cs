using System;
using System.CodeDom;
using System.IO;
using System.Net;
using System.Xml;
using SIM.Instances;
using SIM.Pipelines.InstallModules;
using SIM.Products;

namespace SIM.Pipelines.Install.Modules
{
  public class CreateSolrCore : IPackageInstallActions
  {
    /// <summary>
    /// Wrap WebRequestHelper for unit testing.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public virtual HttpWebResponse RequestAndGetResponse(string url)
    {
      return WebRequestHelper.RequestAndGetResponse(url);
    }

    public virtual void ExecuteSystemCommand(string cmd)
    {
      throw new NotImplementedException();
    }


    public void Execute(Instance instance, Product module)
    {
      XmlDocument config = instance.GetShowconfig();
      string url = config.SelectSingleNode("/sitecore/settings/setting[@name='ContentSearch.Solr.ServiceBaseAddress']").Attributes["value"].Value;

      XmlNodeList solrIndexes =
          config.SelectNodes(
              "/sitecore/contentSearch/configuration/indexes/index[@type='Sitecore.ContentSearch.SolrProvider.SolrSearchIndex, Sitecore.ContentSearch.SolrProvider']");


      string collection1Path = GetCollection1Path(url);
      

      foreach (var node in solrIndexes)
      {
        var element = node as XmlElement;
        if (element == null) continue;
        string id = element.Attributes["id"].InnerText;
        var coreElement = element.SelectSingleNode("param[@desc='core']") as XmlElement;
       
        if (coreElement == null) continue;

        string coreName = coreElement.InnerText.Replace("$(id)", id);

        Log.Info("Core found:"+coreName, this);

        string newCorePath = collection1Path.Replace("collection1", coreName);

        CreateCoreDirectory(collection1Path, newCorePath);

        CreateCorePropertiesFile(coreName, newCorePath);

        CallSolrCreateCoreAPI(url, coreName, newCorePath);
        

         
      }


    }

    private void CallSolrCreateCoreAPI(string url, string coreName, string instanceDir)
    {
      HttpWebResponse response = this.RequestAndGetResponse(string.Format(
        "{0}/admin/cores?action=CREATE&name={1}&instanceDir={2}&config=solrconfig.xml&schema=scheam.xml&dataDir=data", url, coreName, instanceDir));
    }

    private void CreateCorePropertiesFile(string coreName, string newCorePath)
    {

      //TODO Use FileSystem.Local.File
      this.ExecuteSystemCommand(string.Format("echo name={0} > {1}core.properties",coreName,newCorePath));
    }

    private void CreateCoreDirectory(string collection1Path, string newCorePath)
    {
      // TODO Use DirectoryProvider
      this.ExecuteSystemCommand(string.Format("robocopy /e {0} {1}",
        collection1Path,
        newCorePath));
    }

    private string GetCollection1Path(string url)
    {
      var response = this.RequestAndGetResponse(string.Format(
        "{0}/solr/admin/cores", url));

      
      var doc = new XmlDocument();
      doc.Load(response.GetResponseStream());

      XmlNode collection1Node = doc.SelectSingleNode("/response/lst[@name='STATUS']/lst[@name='collection1']");
      if (collection1Node == null) throw new ApplicationException("collection1 not found");

      return collection1Node.SelectSingleNode("str[@name='instanceDir']").InnerText;


    }
  }
}
