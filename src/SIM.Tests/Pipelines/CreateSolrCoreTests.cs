using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SIM.Instances;
using SIM.Pipelines;
using SIM.Pipelines.Install.Modules;
using SIM.Products;

namespace SIM.Tests.Pipelines
{
  [TestClass]
  public class CreateSolrCoreTests
  {
    private CreateSolrCore _sut;
    private Instance _instance;
    private Product _module;

    [TestInitialize]
    public void SetUp()
    {
      _sut = Substitute.For<CreateSolrCore>();
      _instance = Substitute.For<Instance>();
      _module = Substitute.For<Product>();
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(GetConfigXml("SOME_URL", "SOME_CORE_NAME", "SOME_ID"));
      _instance.GetShowconfig().Returns(doc);

    }

    private void Arrange()
    {
      ArrangeGetCores("<lst name='collection1'>"+
                      "<str name='instanceDir'>c:\\some\\path\\collection1\\</str>"+
                      "</lst>");
    }

    private void Act()
    {
      _sut.Execute(_instance, _module);
    }

    [TestMethod]
    public void ShouldGetCores()
    {
      Arrange();

      Act();

      _sut.Received().RequestAndGetResponse("SOME_URL/admin/cores");
    }

    [TestMethod, ExpectedException(typeof(ApplicationException))]
    public void ShouldThrowIfNoCollection1()
    {
      ArrangeGetCores("");

      Act();
    }

    [TestMethod]
    public void ShouldCopyCollection1InstanceDirToNewCorePath()
    {
      Arrange();

      Act();

      _sut.Received().CopyDirectory(@"c:\some\path\collection1\", @"c:\some\path\SOME_CORE_NAME\");
    }

    [TestMethod]
    public void ShouldDeletePropertiesFile()
    {
      Arrange();

      Act();

      _sut.Received().DeleteFile(@"c:\some\path\SOME_CORE_NAME\core.properties");
    }



    [TestMethod]
    public void ShouldCallSolrRE()
    {
      Arrange();

      _sut.Execute(_instance,_module);


      var dirPath = "c:\\some\\path\\SOME_CORE_NAME\\";
      string coreName = "SOME_CORE_NAME";
      _sut.Received()
        .RequestAndGetResponse(
          string.Format(
            "SOME_URL/admin/cores?action=CREATE&name={0}&instanceDir={1}&config=solrconfig.xml&schema=schema.xml&dataDir=data", coreName, dirPath
            ));

    }

    // TODO Copy sitecore optimized Schema.xml 

    private string GetConfigXml(string someUrl, string someCoreName, string someId)
    {
      return "<sitecore>" +
              "<settings>" +
                  string.Format("<setting name='ContentSearch.Solr.ServiceBaseAddress' value='{0}' />", someUrl) +
              "</settings>" +
             "<contentSearch>" +
              "<configuration>" +
                "<indexes>" +
                  string.Format("<index  type='Sitecore.ContentSearch.SolrProvider.SolrSearchIndex, Sitecore.ContentSearch.SolrProvider' id='{0}'>", someId) +

             string.Format("<param desc='core' id='$(id)'>{0}</param>", someCoreName) +
             "</index></indexes></configuration></contentSearch></sitecore>";
    }

    private void ArrangeGetCores(string coreInfo)
    {
      HttpWebResponse response = Substitute.For<HttpWebResponse>();
      string returnValue = string.Format("<response><lst name='status' >{0}</lst></response>", coreInfo);
      var bytes = UTF8Encoding.UTF8.GetBytes(returnValue);
      response.GetResponseStream().Returns(new MemoryStream(bytes));
      _sut.RequestAndGetResponse("SOME_URL/admin/cores").Returns(response);
    }
  }
}
