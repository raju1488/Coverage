using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Report
{
    public class ReportHelper
    {
        public class CodeData
        {
            [BsonId]
            [BsonIgnoreIfDefault]
            public ObjectId _id { get; set; }
            public int asmblycount;
            public int classescount;
            public int files;
            public int coveredLines;
            public int uncoveredLines;
            public int coverablelines;
            public int linecoverage;
            public int branchCoverage;

            public int modifiedasmblycount;
            public int newlines;
            public int testednewlines;
            public int buildcoverage;

            public List<TotAsmbly> asmbly = new List<TotAsmbly>();
        }
        public class TotAsmbly
        {
            public string name;
            public int coveredLines;
            public int uncoveredLines;
            public int coverableLines;
            public int totalLines;
            public int newlines;
            public int testednewlines;
            public string testcoverage;
            public string branchCoverage;
            public string coverageType;
            public string coverage;
            public int coveredBranches;
            public int totalBranches;
        }
        public class Asmbly
        {
            [BsonId]
            public ObjectId _id { get; set; }
            public int ID;
            public string name;
            public List<BuildChanges> buildchanges = new List<BuildChanges>();
            public List<Classes> classes = new List<Classes>();
            public int coveredLines;
            public int uncoveredLines;
            public int coverableLines;
            public int totalLines;
            public int newlines;
            public int testednewlines;
        }
        public class BuildChanges
        {
            public string name;
            public List<string> buildlines = new List<string>();
            public List<string> buildtestedlines = new List<string>();
        }
        public class Classes
        {
            public string name;
            public int coveredLines;
            public int uncoveredLines;
            public int coverableLines;
            public int totalLines;
            public int newlines;
            public int testednewlines;
            public string testcoverage;
            public string coverageType;
            public string coverage;
            public string methodCoverage;
            public string branchCoverage;
            public int coveredBranches;
            public int totalBranches;
            public List<string> buildlines = new List<string>();
            public List<string> lineCoverageHistory = new List<string>();
            public List<string> branchCoverageHistory = new List<string>();
        }
        public static List<Asmbly> ReadJson()
        {
            List<Asmbly> buildlines = new List<Asmbly>();

            try
            {
                string[] Mongo = @System.Configuration.ConfigurationManager.AppSettings["mongodbconstring"].Split(':');
                MongoClientSettings settings = new MongoClientSettings();
                settings.Credentials = new List<MongoCredential>() { new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity("CodeData", Mongo[2]), new PasswordEvidence(Mongo[3])) }; ;
                settings.Server = new MongoServerAddress(Mongo[0], Convert.ToInt32(Mongo[1]));
                MongoClient client = new MongoClient(settings);
                var mongoServer = client.GetDatabase(@System.Configuration.ConfigurationManager.AppSettings["mongodb"]);
                var collection = mongoServer.GetCollection<Asmbly>(@System.Configuration.ConfigurationManager.AppSettings["collection"]);


                var filter = Builders<Asmbly>.Filter.Where(x => x.ID > 0);
                buildlines = collection.Find(filter).ToList();

                return buildlines;
            }
            catch (Exception e)
            {
                return buildlines;
            }
        }
        public static List<Asmbly> ReadAsmbly(string asmblyname)
        {
            List<Asmbly> buildlines = new List<Asmbly>();
            try
            {
                string[] Mongo = @System.Configuration.ConfigurationManager.AppSettings["mongodbconstring"].Split(':');
                MongoClientSettings settings = new MongoClientSettings();
                settings.Credentials = new List<MongoCredential>() { new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity("CodeData", Mongo[2]), new PasswordEvidence(Mongo[3])) }; ;
                settings.Server = new MongoServerAddress(Mongo[0], Convert.ToInt32(Mongo[1]));
                MongoClient client = new MongoClient(settings);
                var mongoServer = client.GetDatabase(@System.Configuration.ConfigurationManager.AppSettings["mongodb"]);
                var collection = mongoServer.GetCollection<Asmbly>(@System.Configuration.ConfigurationManager.AppSettings["collection"]);
                var filter = Builders<Asmbly>.Filter.Where(x => x.name == asmblyname);
                buildlines = collection.Find(filter).ToList();
                return buildlines;
            }
            catch (Exception e)
            {
                return buildlines;
            }
        }

        public static void MongoInsertSum(string jsonstring)
        {
            List<BsonDocument> buildlines = new List<BsonDocument>();
            try
            {
                string[] Mongo = @System.Configuration.ConfigurationManager.AppSettings["mongodbconstring"].Split(':');
                MongoClientSettings settings = new MongoClientSettings();
                settings.Credentials = new List<MongoCredential>() { new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity("CodeData", Mongo[2]), new PasswordEvidence(Mongo[3])) }; ;
                settings.Server = new MongoServerAddress(Mongo[0], Convert.ToInt32(Mongo[1]));
                MongoClient client = new MongoClient(settings);
                var mongoServer = client.GetDatabase(@System.Configuration.ConfigurationManager.AppSettings["mongodb"]);
                var collection = mongoServer.GetCollection<BsonDocument>("CodeSum");
                var filter = Builders<BsonDocument>.Filter.Where(x => true);
                buildlines = collection.Find(filter).ToList();
                var test = JsonConvert.DeserializeObject<CodeData>(jsonstring);
                collection.InsertOne(test.ToBsonDocument());
                var a = new FindOneAndReplaceOptions<BsonDocument>();
                a.IsUpsert = true;
                collection.FindOneAndReplace(filter, test.ToBsonDocument(), a);
            }
            catch (Exception e)
            {

            }

        }
        public static void MongoInsert(string jsonstring)
        {
            List<BsonDocument> buildlines = new List<BsonDocument>();
            try
            {
                string[] Mongo = @System.Configuration.ConfigurationManager.AppSettings["mongodbconstring"].Split(':');
                MongoClientSettings settings = new MongoClientSettings();
                settings.Credentials = new List<MongoCredential>() { new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity("CodeData", Mongo[2]), new PasswordEvidence(Mongo[3])) }; ;
                settings.Server = new MongoServerAddress(Mongo[0], Convert.ToInt32(Mongo[1]));
                MongoClient client = new MongoClient(settings);
                var mongoServer = client.GetDatabase(@System.Configuration.ConfigurationManager.AppSettings["mongodb"]);
                var collection = mongoServer.GetCollection<BsonDocument>(@System.Configuration.ConfigurationManager.AppSettings["collection"]);

                var filter = Builders<BsonDocument>.Filter.Where(x => true);
                buildlines = collection.Find(filter).ToList();
                int[] arrayid = new int[buildlines.Count];
                for (int i = 0; i < buildlines.Count; i++)
                {
                    arrayid[i] = Convert.ToInt32(buildlines[i]["ID"]);
                }
                int maxValue = 0;
                if (arrayid != null && arrayid.Length > 0)
                {
                    maxValue = arrayid.Max();
                }
                var test = JsonConvert.DeserializeObject<Asmbly>(jsonstring);
                test.ID = maxValue + 1;
                collection.InsertOne(test.ToBsonDocument());
            }
            catch (Exception e)
            {

            }

        }
    }
}




