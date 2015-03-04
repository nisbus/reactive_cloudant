using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace ReactiveCloudant.UnitTests
{
    [TestClass]
    public class CloudantSessionTests
    {
        [TestMethod]
        [TestCategory("Constructor")]
        [ExpectedException(typeof(UriFormatException))]
        public void AllNullConstructorTestFailsCreatingUri()
        {
            CloudantSession s = new CloudantSession("");
        }

        [TestMethod]
        [TestCategory("Constructor")]        
        public void InputUrlWithoutTrailingBackslashGetsOneAddedTest()
        {
            CloudantSession s = new CloudantSession("http://cloudant.com");
            Assert.AreEqual("http://cloudant.com/", s.BaseUrl);
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void UsernameAndPasswordAreSetInTheConstructor()
        {
            CloudantSession s = new CloudantSession("http://cloudant.com","username","password");            
            Assert.AreEqual("username", s.Username);
            Assert.AreEqual("password", s.Password);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void TestUUIDReturnValueParsing()
        {
            using (ShimsContext.Create())
            {
                ///Fake the response
                System.Net.Fakes.ShimWebClient.AllInstances.DownloadStringString = (c,u) =>
                    {
                        return "{uuids: [\"364fc3facb6936fd23b22ec8195e923f\"]}";
                    };

                var session = new CloudantSession("https://cloudant.com");
                var id = session.GetID();
                Assert.AreEqual("364fc3facb6936fd23b22ec8195e923f", id);
            };
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void TestSettingIDByGettingOneFromCloudant()
        {
            using (ShimsContext.Create())
            {
                string document = "{\"id\":\"test\"}";
                string expected = "{\"_id\":\"364fc3facb6936fd23b22ec8195e923f\",\"id\":\"test\"}";
                ///Fake the response
                System.Net.Fakes.ShimWebClient.AllInstances.DownloadStringString = (c, u) =>
                {
                    return "{uuids: [\"364fc3facb6936fd23b22ec8195e923f\"]}";
                };

                var session = new CloudantSession("https://cloudant.com");
                var actual = session.SetID(document,string.Empty);
                Assert.AreEqual(expected, actual);
            };
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void TestSettingIDSetByTheUser()
        {
            string document = "{\"id\":\"test\"}";
            string expected = "{\"_id\":\"364fc3facb6936fd23b22ec8195e923f\",\"id\":\"test\"}";
            var session = new CloudantSession("https://cloudant.com");
            var actual = session.SetID(document, "364fc3facb6936fd23b22ec8195e923f");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSettingRevForInvalidJson()
        {            
            
            var session = new CloudantSession("https://cloudant.com");
            session.SetRev("", "364fc3facb6936fd23b22ec8195e923f");            
        }


        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void TestSettingRev()
        {
            string document = "{\"id\":\"test\"}";
            string expected = "{\"_rev\":\"364fc3facb6936fd23b22ec8195e923f\",\"id\":\"test\"}";
            var session = new CloudantSession("https://cloudant.com");
            var actual = session.SetRev(document, "364fc3facb6936fd23b22ec8195e923f");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void TestSettingRevWithEmptyRevisionID()
        {
            string document = "{\"id\":\"test\"}";            
            var session = new CloudantSession("https://cloudant.com");
            var actual = session.SetRev(document, "");
            Assert.AreEqual(document, actual);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSettingIDForEmptyJson()
        {                        
            var session = new CloudantSession("https://cloudant.com");
            session.SetID("", "364fc3facb6936fd23b22ec8195e923f");            
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]        
        public void AllNullReturnsNullWhenSettingQueryParameters()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual(string.Empty, session.SetQueryParameters(null, null, null, false, false, false, 0, 0));
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]        
        [ExpectedException(typeof(ArgumentException))]
        public void KeyAndStartKeyAreMutuallyExclusive()
        {
            var session = new CloudantSession("https://cloudant.com");
            session.SetQueryParameters("key", "startkey", null, false, false, false, 0, 0);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]        
        public void QueryParameterSetKeyOnlyTest()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual("?key=\"key\"", session.SetQueryParameters("key", "", null, false, false, false, 0, 0));
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void QueryParameterSetStartAndEndKeyTest()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual("?startkey=\"start\"&endkey=\"end\"", session.SetQueryParameters("", "start", "end", false, false, false, 0, 0));
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void QueryParameterSetIncludeDocsOnly()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual("?include_docs=true", session.SetQueryParameters("", "", "", true, false, false, 0, 0));
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        public void QueryParameterSetIncludeDocsAndKeys()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual("?include_docs=true&startkey=\"start\"&endkey=\"end\"", session.SetQueryParameters("", "start", "end", true, false, false, 0, 0));
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]
        [ExpectedException(typeof(ArgumentException))]
        public void KeyQueryExceptionWithEndKeyOnly()
        {
            var session = new CloudantSession("https://cloudant.com");
            session.SetQueryParameters("", "", "end", true, false, false, 0, 0);
        }

        [TestMethod]
        [TestCategory("SessionHelpers")]        
        public void KeyQueryStartKeyOnly()
        {
            var session = new CloudantSession("https://cloudant.com");
            Assert.AreEqual("?startkey=\"start\"", session.SetQueryParameters("", "start", "", false, false, false, 0, 0));
        }
    }
}
