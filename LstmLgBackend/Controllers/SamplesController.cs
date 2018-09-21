using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.OData;
using LstmLgBackend.Models;
using System.Collections.Generic;
using System;
using Hangfire;

namespace LstmLgBackend.Controllers
{
    ///<summary>
    /// Controller for actions on samples.
    ///</summary>
    public class SamplesController : ApiController
    {
        private ILstmLgBackendContext lstmDb = new LstmLgBackendContext();

        public SamplesController() { }

        public SamplesController(ILstmLgBackendContext context)
        {
            lstmDb = context;
        }

        ///<summary>
        /// Get all samples of a scenario.intent. Return an empty list if no sample.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent.
        /// </param>
        [Route("api/GetSamples/{ScenarioName}/{IntentName}")]
        public List<Sample> GetSamples(string ScenarioName, string IntentName)
        {
            List<Sample> samples = lstmDb.Samples.Where(e => e.Intent.Scenario.name == ScenarioName && e.Intent.name == IntentName && e.good != false && e.template != null && e.response != null).ToList<Sample>();
            for (int i = 0; i < samples.Count(); i++)
            {
                samples[i].tokens = Token.GenerateTokenList(samples[i].template);
            }
            return samples;

        }

        /// <summary>
        /// Update sample with provided id. Return NotFound if no sample with provided id.
        /// </summary>
        /// <param name="id">
        /// Id of sample to update.
        /// </param>
        /// <param name="sample">
        /// Part of sample object to update(Contained in Request body).
        /// </param>
        [ResponseType(typeof(void))]
        [Route("api/UpdateSample/{id}")]
        [HttpPatch]
        public async Task<IHttpActionResult> UpdateSample(int id, [FromBody] Delta<Sample> sample)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await lstmDb.Samples.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            sample.Patch(entity);
            try
            {
                await lstmDb.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SampleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(entity);
        }

        /// <summary>
        /// Generate template for given sample. Return sample with generated template.
        /// </summary>
        [Route("api/GenerateTemplate/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> GenerateTemplate(string ScenarioName, string IntentName, Sample sample)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            Intent intent = lstmDb.Intents.SingleOrDefault(e => e.Scenario.name == ScenarioName && e.name == IntentName);
            if (intent == null)
            {
                return BadRequest("No Such Intent");
            }
            List<SlotDescription> slotDescriptions;
            try
            {
                slotDescriptions = intent.mySlotDescriptions;
            }
            catch
            {
                throw new Exception("no intent.slotDescriptioin");
            }
            sample.GenerateTemplate(slotDescriptions);
            sample.tokens = Token.GenerateTokenList(sample.template);
            await lstmDb.SaveChangesAsync();
            return Ok(sample);
        }

        ///<summary>
        /// Add a sample to scenario.intent. Return BadRequest("No Such Scenario") if no scenario with that name.
        /// Return BadRequest("No Such Intent") if no intent with that name.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent.
        /// </param>
        /// <param name="sample">
        /// The sample to add.
        /// </param>
        [ResponseType(typeof(Sample))]
        [Route("api/AddSample/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> AddSample(string ScenarioName, string IntentName, Sample sample)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            Intent intent = lstmDb.Intents.SingleOrDefault(e => e.Scenario.name == ScenarioName && e.name == IntentName);
            if (intent == null)
            {
                return BadRequest("No Such Intent");
            }
            sample.intentID = intent.id;

            lstmDb.Samples.Add(sample);
            await lstmDb.SaveChangesAsync();
            return Created("DefaultApi", sample);
        }

        //TODO : hangfire
        [Route("api/AddBatchSample/{ScenarioName}/{IntentName}")]
        public IHttpActionResult AddBatchSample(string ScenarioName, string IntentName, List<Sample> samples)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            Intent intent = lstmDb.Intents.SingleOrDefault(e => e.Scenario.name == ScenarioName && e.name == IntentName);
            if (intent == null)
            {
                return BadRequest("No Such Intent");
            }
            //pending
            if(intent.batchStatus == 1)
            {
                return BadRequest("Intent is under batch add");
            }
            BackgroundJob.Enqueue(() => batchAdd(intent.id, samples));
            return Ok();
        }

        public void batchAdd(int intentID, List<Sample> samples)
        {
            Intent intent = lstmDb.Intents.Find(intentID);
            intent.batchStatus = 1;
            lstmDb.SaveChanges();
            bool success = true;
            try
            {
                foreach (Sample sample in samples)
                {
                    sample.intentID = intent.id;
                    lstmDb.Samples.Add(sample);
                }
                lstmDb.SaveChanges();
            }
            catch
            {
                success = false;
                intent.batchStatus = 3;
            }
            if (success == true)
            {
                intent.batchStatus = 2;
            }
            lstmDb.SaveChanges();
        }

        [HttpGet]
        [Route("api/CheckBatchStatus/{ScenarioName}/{IntentName}")]
        public IHttpActionResult CheckBatchStatus(string ScenarioName, string IntentName)
        {
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }
            Intent intent = lstmDb.Intents.SingleOrDefault(e => e.scenarioID == scenario.id && e.name == IntentName);
            if (intent == null)
            {
                return BadRequest("No Such Intent");
            }
            return Ok(intent.batchStatus);
        }

        ///<summary>
        /// Delete a sample with provided id. Return NotFound if no sample with that id.
        ///</summary>
        ///<param name = "id">
        /// The id of sample to delete.
        /// </param>
        [ResponseType(typeof(Sample))]
        [Route("api/DeleteSample/{id}")]
        public async Task<IHttpActionResult> DeleteSample(int id)
        {
            Sample sample = await lstmDb.Samples.FindAsync(id);
            if (sample == null)
            {
                return NotFound();
            }
            lstmDb.Samples.Remove(sample);
            await lstmDb.SaveChangesAsync();

            return Ok(sample);
        }

        [Route("api/GetOneUnAnnotatedSample/{ScenarioName}/{IntentName}")]
        public IHttpActionResult GetOneUnAnnotatedSample(string ScenarioName, string IntentName)
        {
            IQueryable<Sample> samples = lstmDb.Samples.Where(e => e.Intent.Scenario.name == ScenarioName && e.Intent.name == IntentName);
            foreach (Sample sample in samples)
            {
                if (sample.response == null && sample.template == null)
                {
                    return Ok(sample);
                }
            }
            return Ok("No UnAnnotatedSample");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lstmDb.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool SampleExists(int id)
        {
            return lstmDb.Samples.Count(e => e.id == id) > 0;
        }
    }
}