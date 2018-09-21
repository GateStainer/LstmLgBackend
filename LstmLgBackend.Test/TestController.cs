using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LstmLgBackend.Controllers;
using LstmLgBackend.Models;
using System.Web.Http.Results;
using System.Linq;
using System.Threading.Tasks;
using LstmLgBackend.Test;
using System.Web.Http;

namespace LstmLgBackend.Test
{
    [TestClass]
    public class TestController
    {
        [TestMethod]
        public void GetScenarios_ShouldReturnAllScenarios()
        {
            var context = new TestLstmLgBackendContext();

            context.Scenarios.Add(new Scenario { id = 1, name = "nba" });
            context.Scenarios.Add(new Scenario { id = 2, name = "stock" });

            var controller = new ScenariosController(context);

            var result = controller.GetScenarios() as TestScenarioDbSet;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Local.Count);
            
            
      
        }

        [TestMethod]
        public void CreateScenario_ShouldAddSameScenario()
        {
            var context = new TestLstmLgBackendContext();

            var controller = new ScenariosController(context);

           
            //var result1 = controller.CreateScenario("nba");

            //var result2 = controller.CreateScenario("nba");
       


            
            //var Scenario = context.Scenarios;

            //Assert.AreEqual(Scenario.Count(), 1);

        }

        [TestMethod]
        public void DeleteScenario_ShouldDeleteSameScenario()
        {
            var context = new TestLstmLgBackendContext();
            context.Scenarios.Add(new Scenario { id = 1, name = "nba" });

            var controller = new ScenariosController(context);
            controller.DeleteScenario("nba");

            var Scenario = context.Scenarios.FirstOrDefault();
            Assert.IsNull(Scenario);

            context.Scenarios.Add(new Scenario { id = 2, name = "cba" });
            context.Scenarios.Add(new Scenario { id = 3, name = "fba" });
            controller.DeleteScenario("cba");

            var Scenarios = context.Scenarios;
            Assert.AreEqual(Scenarios.Count(), 1);
            Assert.AreEqual(Scenarios.First().name, "fba");
        }

        [TestMethod]
        public void GetIntents_ShouldReturnAllIntents()
        {
            var context = new TestLstmLgBackendContext();
            context.Intents.Add(new Intent { id = 1, name = "test", Scenario = new Scenario { id = 1, name = "nba" } });
            context.Intents.Add(new Intent { id = 2, name = "test2", Scenario = new Scenario { id = 2, name = "cbs" } });
            var controller = new IntentsController(context);
            var result = controller.GetIntents("nba");
            Assert.AreEqual(result.Count(), 1);
            Assert.AreEqual(result.First().name, "test");
            Assert.AreEqual(result.First().id, 1);
            Assert.AreEqual(result.First().Scenario.name, "nba");
            

        }

        [TestMethod]
        public void CreateIntent_ShouldAddSameIntent()
        {
            var context = new TestLstmLgBackendContext();
            var controller = new IntentsController(context);
            Intent intent = new Intent();
            intent.name = "test";
            controller.CreateIntent("nba", intent);
            var Intent = context.Intents.FirstOrDefault();
            Assert.IsNull(Intent);

            context.Scenarios.Add(new Scenario { id = 1, name = "nba" });
            controller.CreateIntent("nba", intent);
            var Intent2 = context.Intents.FirstOrDefault();
            Assert.IsNotNull(Intent2);
            Assert.AreEqual(Intent2.name, "test");
            Assert.AreEqual(Intent2.scenarioID, 1);
        }


        [TestMethod]
        public void DeleteIntent_ShouldDeleteSameIntent()
        {
            var context = new TestLstmLgBackendContext();
            var controller = new IntentsController(context);

            context.Intents.Add(new Intent { id = 1, name = "test" ,scenarioID = 1});
            controller.DeleteIntent("nba", "test");
            var Intent = context.Intents.FirstOrDefault();
            Assert.AreEqual(Intent.name, "test");

            context.Scenarios.Add(new Scenario { id = 1, name = "nba" });
            controller.DeleteIntent("nba", "test");
            Assert.IsNull(context.Intents.FirstOrDefault());

            context.Intents.Add(new Intent { id = 1, name = "test", scenarioID = 1 });
            context.Samples.Add(new Sample { id = 1, intentID = 1 });
            context.Samples.Add(new Sample { id = 2, intentID = 1 });
            context.Samples.Add(new Sample { id = 3, intentID = 1 });
            controller.DeleteIntent("nba", "test");
            Assert.AreEqual(context.Scenarios.FirstOrDefault().name, "nba");
            Assert.IsNull(context.Intents.FirstOrDefault());
            Assert.AreEqual(context.Samples.Count(), 2);


        }

        Scenario GetDemoScenario()
        {
            return new Scenario() { id = 3, name = "nba" };
        }


    }
}
