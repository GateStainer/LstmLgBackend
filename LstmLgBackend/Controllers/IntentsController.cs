using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using LSTMDecWrapperSurfaceRealizer;
using System.IO;
using Newtonsoft.Json;
using LstmLgBackend.Models;
using System.Diagnostics;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using System.Web;
using Hangfire;

namespace LstmLgBackend.Controllers
{
    ///<summary>
    /// Controller for actions on Intents
    ///</summary>
    public class IntentsController : ApiController
    {
        public static int unique_key = 0;
        private static string workDir = (System.Web.Hosting.HostingEnvironment.MapPath("~") + "SCLSTM4Integrate");
        private ILstmLgBackendContext lstmDb = new LstmLgBackendContext();

        public IntentsController()
        {
        }
        public IntentsController(ILstmLgBackendContext context)
        {
            lstmDb = context;
        }

        ///<summary>
        /// Get all intents under one scenario. Return an empty list if no intent.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        [ResponseType(typeof(IQueryable<Intent>))]
        [Route("api/GetIntents/{ScenarioName}")]
        public IQueryable<Intent> GetIntents(string ScenarioName)
        {
            IQueryable<Intent> intents;
            intents = lstmDb.Intents.Where(e => e.Scenario.name == ScenarioName);
            return intents;

        }

        ///<summary>
        /// Create a new intent under one scenario. Return BadRequest("No Such Scenario) if no scenario with the name.
        /// Return BadRequest("Intent already exists") if already exists a intent with same name.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="intent">
        /// The name of intent to create.
        /// </param>
        [ResponseType(typeof(Intent))]
        [Route("api/CreateIntent/{ScenarioName}")]
        public async Task<IHttpActionResult> CreateIntent([FromUri] string ScenarioName, [FromBody] Intent intent)
        {
            if (intent.name == null)
            {
                return BadRequest("Without intent name");
            }
            Scenario scenario = lstmDb.Scenarios.SingleOrDefault(e => e.name == ScenarioName);
            if (scenario == null)
            {
                return BadRequest("No Such Scenario");
            }


            if (lstmDb.Intents.Count(e => e.Scenario.name == scenario.name && e.name == intent.name) > 0)
            {
                return BadRequest("Intent already exists");
            }
            var mySlotDescriptions = string.IsNullOrEmpty(intent.slotDescriptions) ?
                    new List<SlotDescription>() :
                    JsonConvert.DeserializeObject<List<SlotDescription>>(intent.slotDescriptions);
            foreach (SlotDescription slotDescription in mySlotDescriptions)
            {
                if (slotDescription.name.Contains(' '))
                {
                    return BadRequest("Slot name can't contain space");
                }
            }
            //Default DateTime in database
            intent.lastTrainTime = Convert.ToDateTime("1900-01-01T00:00:00.000");
            intent.scenarioID = scenario.id;
            lstmDb.Intents.Add(intent);
            await lstmDb.SaveChangesAsync();
            intent = lstmDb.Intents.SingleOrDefault(e => e.scenarioID == scenario.id && e.name == intent.name);
            return Created("DefaultApi", intent);
        }

        ///<summary>
        /// Delete an intent under one scenario. Return BadRequest("No Such Scenario) if no scenario with the name.
        /// Return BadRequest("No Such Intent") if no intent with the name.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent to delete.
        /// </param>
        [ResponseType(typeof(Intent))]
        [Route("api/DeleteIntent/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> DeleteIntent([FromUri] string ScenarioName, [FromUri] string IntentName)
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
            IQueryable<Sample> samples = lstmDb.Samples.Where(e => e.intentID == intent.id);

            lstmDb.Intents.Remove(intent);
            foreach (Sample sample in samples)
            {
                lstmDb.Samples.Remove(sample);
            }
            await lstmDb.SaveChangesAsync();
            return Ok(intent);
        }

        ///<summary>
        /// Update the slot description of an intent. Post string of new description in Request body.
        /// Return BadRequest("No Such Scenario) if no scenario with the name.
        /// Return BadRequest("No Such Intent") if no intent with the name.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent to update.
        /// </param>
        [ResponseType(typeof(Intent))]
        [Route("api/UpdateSlotDescription/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> UpdateSlotDescription([FromUri] string ScenarioName, [FromUri] string IntentName)
        {

            string description = await Request.Content.ReadAsStringAsync();
            //Don't allow space
            var mySlotDescriptions = string.IsNullOrEmpty(description) ?
                    new List<SlotDescription>() :
                    JsonConvert.DeserializeObject<List<SlotDescription>>(description);
            foreach (SlotDescription slotDescription in mySlotDescriptions)
            {
                if (slotDescription.name.Contains(' '))
                {
                    return BadRequest("Slot name can't contain space");
                }
            }
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
            intent.slotDescriptions = description;
            await lstmDb.SaveChangesAsync();

            return Ok(intent);
        }

        [ResponseType(typeof(Intent))]
        [Route("api/UpdateIntentDescription/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> UpdateIntentDescription([FromUri] string ScenarioName, [FromUri] string IntentName)
        {

            string description = await Request.Content.ReadAsStringAsync();
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
            intent.description = description;
            await lstmDb.SaveChangesAsync();
            return Ok(intent);
        }

        [ResponseType(typeof(Intent))]
        [Route("api/UpdateIntentDescription/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> UpdateIntentName([FromUri] string ScenarioName, [FromUri] string IntentName)
        {

            string newName = await Request.Content.ReadAsStringAsync();
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
            if(newName == IntentName)
            {
                return BadRequest("Please provide a different name");
            }
            Intent otherIntent = lstmDb.Intents.SingleOrDefault(e => e.scenarioID == scenario.id && e.name == newName);
            //Exists other Intent with same name
            if (otherIntent != null)
            {
                return BadRequest("Exist other Intent with same name");
            }
            intent.name = newName;
            await lstmDb.SaveChangesAsync();
            return Ok(intent);
        }

        ///<summary>
        /// Train the model of an intent.
        /// Return BadRequest("No Such Scenario) if no scenario with the name.
        /// Return BadRequest("No Such Intent") if no intent with the name.
        /// Return Exception if training failed.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent to train.
        /// </param>
        [Route("api/Train/{ScenarioName}/{IntentName}")]
        public IHttpActionResult Train(string ScenarioName, string IntentName)
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
            //pending
            if(intent.modelStatus == 1)
            {
                return BadRequest("Intent is under training now");
            }
            IQueryable<Sample> samples = lstmDb.Samples.Where(e => (e.intentID == intent.id && e.good == true && e.template != null));
            if (!samples.Any())
            {
                return BadRequest("Please Add Samples first!");
            }
            var slotDescriptions = intent.mySlotDescriptions;
            int number = unique_key++;
            string dataDir = workDir + @"\" + number;
            Directory.CreateDirectory(dataDir);
            string textTrainPath = dataDir + @"\text.train";
            string svTrainPath = dataDir + @"\sv_train.txt";
            string svDictPath = dataDir + @"\slot_value.txt";
            string tokenPath = dataDir + @"\token.txt";
            string modelPath = dataDir + @"\lm.dnn";
            StreamWriter textTrainWriter = File.CreateText(textTrainPath);
            StreamWriter svTrainWriter = File.CreateText(svTrainPath);
            foreach (Sample sample in samples)
            {
                string text_train = null;
                string sv_train = null;
                //Make First letter of the sentence ToLower
                //Today’s weather is good. All day is sunny. ===> today's weather is good. all day is sunny.
                string template = sample.template;
                try
                {
                    template = char.ToLower(template[0]).ToString() + template.Substring(1);
                }
                catch
                {
                    Directory.Delete(dataDir, true);
                    throw new Exception("Empty response");
                }

                List<char> sentenceEndMark = new List<char> { '.', '?', ';', '!', };
                bool newSentence = false;
                for (int i = 0; i < template.Length; i++)
                {
                    if (sentenceEndMark.Contains(template[i]))
                    {
                        newSentence = true;
                    }
                    else if (newSentence == true)
                    {
                        if (template[i] == ' ')
                        {
                            continue;
                        }
                        else
                        {
                            newSentence = false;
                            //If only "All day is sunny! A"
                            try
                            {
                                template = template.Substring(0, i) + char.ToLower(template[i]) + template.Substring(i + 1);
                            }
                            catch
                            {
                                template = template.Substring(0, i) + char.ToLower(template[i]);
                            }
                        }
                    }
                }
                List<Token> tokens = Token.GenerateTokenList(template);
                foreach (Token token in tokens)
                {
                    text_train += template.Substring(token.start, token.length);
                    if (token.needSpaceBefore == true)
                    {
                        text_train += "###T";
                    }
                    else
                    {
                        text_train += "###F";
                    }
                    text_train += ' ';
                }
                //Example:Tony###T is###T 20###T years###T old###T .###F
                textTrainWriter.WriteLine(text_train);
                foreach (SVPair svPair in sample.mySVPairs)
                {
                    sv_train += svPair.slot;
                    bool condition = false;
                    foreach (SlotDescription des in slotDescriptions)
                    {
                        if (des.name == svPair.slot && des.condition == true)
                        {
                            condition = true;
                        }
                    }
                    if (condition)
                    {
                        //python code needs this format
                        sv_train += "(@C)";
                    }
                    sv_train += ("=\"" + svPair.value + "\",");
                }
                //Example: name="Tony",youth(@C)="True",age="20"
                svTrainWriter.WriteLine(sv_train);
            }
            textTrainWriter.Close();
            svTrainWriter.Close();
            //Hangfire starts
            BackgroundJob.Enqueue(() => StartTrain(intent.id, number, textTrainPath, svTrainPath, svDictPath, tokenPath, modelPath));
            return Ok();
        }

        [HttpGet]
        [Route("api/CheckModelStatus/{ScenarioName}/{IntentName}")]
        public IHttpActionResult CheckModelStatus(string ScenarioName, string IntentName)
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
            return Ok(intent.modelStatus);
        }

        ///<summary>
        /// Using model to generate response(template) for given sample.
        /// Return BadRequest("No Such Scenario) if no scenario with the name.
        /// Return BadRequest("No Such Intent") if no intent with the name.
        /// Return Exception if testing failed.
        ///</summary>
        ///<param name="ScenarioName">
        /// The name of scenario.
        /// </param>
        /// <param name="IntentName">
        /// The name of intent to train.
        /// </param>
        /// <param name="n">
        /// Number of responses to generate.
        /// </param>
        [Route("api/Test/{ScenarioName}/{IntentName}/{n}")]
        public async Task<IHttpActionResult> Test(string ScenarioName, string IntentName, int n)
        {
            List<string> topn = new List<string>();
            LSTMDecSurfaceRealizer rnnlg = new LSTMDecSurfaceRealizer();
            int number = unique_key++;
            string svDictPath = workDir + @"\train\" + number.ToString() + "slot_value.txt";
            string tokenPath = workDir + @"\train\" + number.ToString() + "token.txt";
            string modelPath = workDir + @"\models\" + number.ToString() + "lm.dnn";
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
            if (intent.sv_dict == null || intent.token == null || intent.model == null)
            {
                return BadRequest("Please train model first");
            }
            System.IO.File.WriteAllBytes(svDictPath, intent.sv_dict);
            System.IO.File.WriteAllBytes(tokenPath, intent.token);
            System.IO.File.WriteAllBytes(modelPath, intent.model);
            try
            {
                rnnlg.Initialize(modelPath, tokenPath, svDictPath);
                var slotDescriptions = intent.mySlotDescriptions;
                IList<String> templateStringList = new List<String>();
                IList<String> responseStringList = new List<String>();
                Dictionary<String, String> featureList = new Dictionary<String, String>();
                string svPair = await Request.Content.ReadAsStringAsync();
                List<SVPair> svpairs = JsonConvert.DeserializeObject<List<SVPair>>(svPair);
                foreach (SVPair svpair in svpairs)
                {
                    bool condition = false;
                    foreach (SlotDescription des in slotDescriptions)
                    {
                        if (des.name == svpair.slot && des.condition == true)
                        {
                            condition = true;
                            break;
                        }
                    }
                    if (condition)
                    {
                        featureList.Add((svpair.slot + "(@C)"), svpair.value);
                    }
                    else
                    {
                        featureList.Add(svpair.slot, svpair.value);
                    }
                }
                //return Ok(featureList);
                rnnlg.ApplyBeamSearch(featureList, ref templateStringList, ref responseStringList, n);
                await lstmDb.SaveChangesAsync();
                Dictionary<String, IList<String>> answer = new Dictionary<string, IList<string>>();
                answer.Add("template", templateStringList);
                answer.Add("response", responseStringList);
                File.Delete(svDictPath);
                File.Delete(tokenPath);
                File.Delete(modelPath);
                return Ok(answer);
            }
            catch
            {
                File.Delete(svDictPath);
                File.Delete(tokenPath);
                File.Delete(modelPath);
                return BadRequest("Test failed");
            }
        }

        //Not used now
        [Route("api/Preview/{ScenarioName}/{IntentName}")]
        public async Task<IHttpActionResult> Preview(string ScenarioName, string IntentName)
        {
            LSTMDecSurfaceRealizer rnnlg = new LSTMDecSurfaceRealizer();
            int number = unique_key++;
            string svDictPath = workDir + @"\train\" + number.ToString() + "slot_value.txt";
            string tokenPath = workDir + @"\train\" + number.ToString() + "token.txt";
            string modelPath = workDir + @"\models\" + number.ToString() + "lm.dnn";
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

            List<Sample> samples = lstmDb.Samples.Where(e => e.intentID == intent.id && e.response == null && e.template == null).ToList<Sample>();
            List<Sample> temp_samples = new List<Sample>();

            if (intent.sv_dict == null || intent.token == null || intent.model == null)
            {
                return BadRequest("Please train model first");
            }
            System.IO.File.WriteAllBytes(svDictPath, intent.sv_dict);
            System.IO.File.WriteAllBytes(tokenPath, intent.token);
            System.IO.File.WriteAllBytes(modelPath, intent.model);
            rnnlg.Initialize(modelPath, tokenPath, svDictPath);
            var slotDescriptions = intent.mySlotDescriptions;
            foreach (Sample sample in samples)
            {
                IList<String> templateStringList = new List<String>();
                IList<String> responseStringList = new List<String>();
                Dictionary<String, String> featureList = new Dictionary<String, String>();
                string svPair = sample.slotValuePair;
                List<SVPair> svpairs = JsonConvert.DeserializeObject<List<SVPair>>(svPair);
                foreach (SVPair svpair in svpairs)
                {
                    bool condition = false;
                    foreach (SlotDescription des in slotDescriptions)
                    {
                        if (des.name == svpair.slot && des.condition == true)
                        {
                            condition = true;
                            break;
                        }
                    }
                    if (condition)
                    {
                        featureList.Add((svpair.slot + "(@C)"), svpair.value);
                    }
                    else
                    {
                        featureList.Add(svpair.slot, svpair.value);
                    }
                }
                rnnlg.ApplyBeamSearch(featureList, ref templateStringList, ref responseStringList, 1);
                Sample temp = new Sample(sample);
                temp.response = responseStringList[0];
                temp.template = templateStringList[0];
                temp_samples.Add(temp);
            }
            await lstmDb.SaveChangesAsync();
            try
            {
                System.IO.File.Delete(svDictPath);
                System.IO.File.Delete(tokenPath);
                System.IO.File.Delete(modelPath);
            }
            catch
            {
            }
            return Ok(temp_samples);
        }

        [AutomaticRetry(Attempts = 0)]
        public void StartTrain(int intentID, int number, string textTrainPath, string svTrainPath, string svDictPath, string tokenPath, string modelPath)
        {
            Intent intent = lstmDb.Intents.Find(intentID);
            intent.modelStatus = 1;
            lstmDb.SaveChanges();
            bool success = true;
            try
            {
                FileOperation.ExecutePython(workDir, number);
            }
            catch
            {
                success = false;
                intent.modelStatus = 3;
            }
            if (success == true)
            {
                intent.modelStatus = 2;
                intent.sv_dict = System.IO.File.ReadAllBytes(svDictPath);
                intent.token = System.IO.File.ReadAllBytes(tokenPath);
                intent.model = System.IO.File.ReadAllBytes(modelPath);
            }
            intent.lastTrainTime = DateTime.UtcNow;
            lstmDb.SaveChanges();
            Directory.Delete(workDir + @"\" + number, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lstmDb.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool IntentExists(int id)
        {
            return lstmDb.Intents.Count(e => e.id == id) > 0;
        }
    }
}