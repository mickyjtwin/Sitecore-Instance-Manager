using System;
using System.CodeDom;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using SIM.Instances;
using SIM.Pipelines.InstallModules;
using SIM.Products;

namespace SIM.Pipelines.Install.Modules
{
  public class CreateSolrCore : IPackageInstallActions
  {
    private const string DefaultCollectionName = "collection1";

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
      System.Diagnostics.Process.Start(cmd);  //TODO Replace with SIM utilities.
    }


    public void Execute(Instance instance, Product module)
    {
      XmlDocument config = instance.GetShowconfig();
      string url = config.SelectSingleNode("/sitecore/settings/setting[@name='ContentSearch.Solr.ServiceBaseAddress']").Attributes["value"].Value;

      XmlNodeList solrIndexes =
          config.SelectNodes(
              "/sitecore/contentSearch/configuration/indexes/index[@type='Sitecore.ContentSearch.SolrProvider.SolrSearchIndex, Sitecore.ContentSearch.SolrProvider']");


      string defaultCollectionPath = GetDefaultCollectionPath(url);
      

      foreach (XmlElement node in solrIndexes)
      {
        var coreName = GetCoreName(node);

        string corePath = defaultCollectionPath.Replace(DefaultCollectionName, coreName);

        this.CopyDirectory(defaultCollectionPath, corePath);

        DeleteCopiedCorePropertiesFile(corePath);

        UpdateSchema(instance.WebRootPath, corePath);

        CallSolrCreateCoreAPI(url, coreName, corePath);
         
      }


    }

    private void UpdateSchema(string webRootPath, string corePath)
    {
      string contentSearchDllPath = webRootPath.EnsureEnd(@"\") + @"bin\Sitecore.ContentSearch.dll";
      string schemaPath = corePath.EnsureEnd(@"\") + @"conf\schema.xml"; 
      this.GenerateSchema(contentSearchDllPath, schemaPath);
    }

    private static string GetCoreName(XmlElement node)
    {
      var coreElement = node.SelectSingleNode("param[@desc='core']") as XmlElement;
      string id = node.Attributes["id"].InnerText;
      string coreName = coreElement.InnerText.Replace("$(id)", id);
      return coreName;
    }

    private void CallSolrCreateCoreAPI(string url, string coreName, string instanceDir)
    {
      HttpWebResponse response = this.RequestAndGetResponse(string.Format(
        "{0}/admin/cores?action=CREATE&name={1}&instanceDir={2}&config=solrconfig.xml&schema=schema.xml&dataDir=data", url, coreName, instanceDir));
    }

    private void DeleteCopiedCorePropertiesFile(string newCorePath)
    {
      string path = string.Format(newCorePath.EnsureEnd(@"\") + "core.properties");
      this.DeleteFile(path);
       
    }

    private string GetDefaultCollectionPath(string url)
    {
      var response = this.RequestAndGetResponse(string.Format(
        "{0}/admin/cores", url));

      
      var doc = new XmlDocument();
      doc.Load(response.GetResponseStream());

      XmlNode collection1Node = doc.SelectSingleNode("/response/lst[@name='status']/lst[@name='collection1']");
      if (collection1Node == null) throw new ApplicationException("collection1 not found");

      return collection1Node.SelectSingleNode("str[@name='instanceDir']").InnerText;


    }

    #region System calls are virtual for unit testing

    public virtual void CopyDirectory(string sourcePath, string destinationPath)
    {
      FileSystem.FileSystem.Local.Directory.Copy(sourcePath, destinationPath, recursive:true);
    }

    public virtual void WriteAllText(string path, string text)
    {
      FileSystem.FileSystem.Local.File.WriteAllText(path, text);
    }

    public virtual void DeleteFile(string path)
    {
      FileSystem.FileSystem.Local.File.Delete(path);
    }


    /// <summary>
    /// Dynamically loads GenerateSchema class from target site.
    /// See https://msdn.microsoft.com/en-us/library/1009fa28(v=vs.110).aspx
    /// https://msdn.microsoft.com/en-us/library/system.reflection.methodinfo.invoke(v=vs.110).aspx
    /// </summary>
    /// <param name="dllPath"></param>
    /// <param name="schemaPath"></param>

    public virtual void GenerateSchema(string dllPath, string schemaPath)
    {
      var assembly = Assembly.LoadFrom(dllPath);
      var generateSchema = assembly.GetType("Sitecore.ContentSearch.ProviderSupport.Solr.SchemaGenerator");
      var obj = Activator.CreateInstance(generateSchema);
      var method = generateSchema.GetMethod("GenerateSchema");
      method.Invoke(obj, new object[] {schemaPath, schemaPath});
    }

    #endregion
  }
}
