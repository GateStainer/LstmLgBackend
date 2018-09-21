using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using LstmLgBackend.Models;

namespace LstmLgBackend.Controllers
{
    ///<summary>
    /// Controller for actions on Scenarios
    ///</summary>
    public class ScenariosController : ApiController
    {
        private ILstmLgBackendContext lstmDb = new LstmLgBackendContext();
        public ScenariosController() { }
        public ScenariosController(ILstmLgBackendContext context)
        {
            lstmDb = context;
        }

        /// <summary>
        /// Get all Scenarios.
        /// </summary>
        [ResponseType(typeof(IQueryable<Scenario>))]
        [Route("api/GetScenarios")]
        public IQueryable<Scenario> GetScenarios()
        {

            return lstmDb.Scenarios;
        }

        /// <summary>
        /// Create a new Scenario by name. Return BadRequest("Scenario already exists") if already exists a scenario with same name.
        /// </summary>
        [ResponseType(typeof(Scenario))]
        [Route("api/CreateScenario")]
        public async Task<IHttpActionResult> CreateScenario([FromBody] Scenario scenario)
        {
            if (lstmDb.Scenarios.Count(e => e.name == scenario.name) > 0)
            {
                return BadRequest("Scenario already exists");
            }
            lstmDb.Scenarios.Add(scenario);
            await lstmDb.SaveChangesAsync();
            Scenario temp = lstmDb.Scenarios.SingleOrDefault(e => e.name == scenario.name);
            return Created("DefaultApi", temp);
        }

        /// <summary>
        /// Delete a Scenario by name. Will also delete all intents and samples under that scenario. Return NotFound if no scenario with provided name.
        /// </summary>
        /// <param name="name">The name of Scenario to delete</param>
        [ResponseType(typeof(Scenario))]
        [Route("api/DeleteScenario/{name}")]
        public async Task<IHttpActionResult> DeleteScenario(string name)
        {
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == name);
            if (scenario == null)
            {
                return NotFound();
            }
            IQueryable<Intent> intents = lstmDb.Intents.Where(e => e.scenarioID == scenario.id);
            foreach (Intent intent in intents)
            {
                IQueryable<Sample> samples = lstmDb.Samples.Where(w => w.intentID == intent.id);
                foreach (Sample sample in samples)
                {
                    lstmDb.Samples.Remove(sample);
                }
                lstmDb.Intents.Remove(intent);
            }
            lstmDb.Scenarios.Remove(scenario);
            await lstmDb.SaveChangesAsync();

            return Ok(scenario);
        }

        [ResponseType(typeof(Scenario))]
        [Route("api/UpdateScenarioDescription/{ScenarioName}")]
        public async Task<IHttpActionResult> UpdateScenarioDescription([FromUri] string ScenarioName)
        {

            string description = await Request.Content.ReadAsStringAsync();
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            scenario.description = description;
            await lstmDb.SaveChangesAsync();
            return Ok(scenario);
        }

        [ResponseType(typeof(Scenario))]
        [Route("api/UpdateScenarioName/{ScenarioName}")]
        public async Task<IHttpActionResult> UpdateScenarioName([FromUri] string ScenarioName)
        {

            string newName = await Request.Content.ReadAsStringAsync();
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            if(newName == ScenarioName)
            {
                return BadRequest("Please provide a different name");
            }
            Scenario otherScenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == newName);
            //Exists other Scenario with same name
            if(otherScenario != null)
            {
                return BadRequest("Exist other Scenario with same name");
            }
            scenario.name = newName;
            await lstmDb.SaveChangesAsync();
            return Ok(scenario);
        }

        ///<summary>
        /// 
        ///</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lstmDb.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ScenarioExists(int id)
        {
            return lstmDb.Scenarios.Count(e => e.id == id) > 0;
        }
    }
}