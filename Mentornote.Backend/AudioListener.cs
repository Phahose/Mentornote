#nullable disable
using Azure.Core;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Mentornote.Backend;
using Mentornote.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Mentornote.Backend
{
    /// <summary>
    /// Captures system audio (loopback) and emits both:
    ///  - full WAV file at the end (AudioFileReady)
    ///  - small byte[] chunks during recording (AudioChunkReady)
    /// </summary>
    public class AudioListener : IDisposable
    {
        private WasapiLoopbackCapture _capture;
        private WaveFileWriter _writer;
        private string _tempFile;

        private readonly List<byte> _buffer = new();          
        private readonly int _chunkSeconds = 1;              

        // This is an event it is not a regular declaration
        // It says "when the audio file is ready and when we have a new chunck of audi ready for processig, notify anyone who is listening"
        public event EventHandler<string> AudioFileReady;     // full file ready
        public event EventHandler<byte[]> AudioChunkReady;    // chunk ready for real-time processing
        private readonly Transcribe _transcribe;
        public event EventHandler<string> TranscriptReady;
        private readonly List<string> _transcriptHistory = new();

        public string FullMeetingTranscript;





        public int _appointmentId;
        public AudioListener(Transcribe transcribe)
        {
            _transcribe = transcribe;
        }

        public void StartListening(int appointmentId)
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"meeting_{Guid.NewGuid()}.wav");


            _capture = new WasapiLoopbackCapture(); //start  capture system audio
            _writer = new WaveFileWriter(_tempFile, _capture.WaveFormat);

            _appointmentId = appointmentId;
            _capture.DataAvailable += Capture_DataAvailable;

            _capture.RecordingStopped += Capture_RecordingStopped;


            _capture.StartRecording();
            Console.WriteLine("Listening started...");
        }

        private async void  Capture_DataAvailable(object sender, WaveInEventArgs e)
        {

            try
            {
                // 1️  keep writing to the full meeting file
                _writer.Write(e.Buffer, 0, e.BytesRecorded);

                // 2️  collect data in memory for small chunks
                lock (_buffer)
                {
                    _buffer.AddRange(e.Buffer[..e.BytesRecorded]);
                }

                // 3️  if buffer > N seconds, fire chunk event
                int bytesPerSecond = _capture.WaveFormat.AverageBytesPerSecond;
                if (_buffer.Count >= bytesPerSecond * _chunkSeconds)
                {
                    byte[] chunk;
                    lock (_buffer)
                    {
                        chunk = _buffer.ToArray();
                        _buffer.Clear();
                    }

                    // Wrap the raw PCM chunk as a valid WAV (in-memory) using the SAME format
                    byte[] wavChunk;
                    using (var ms = new MemoryStream())
                    {
                        using (var w = new WaveFileWriter(ms, _capture.WaveFormat))
                        {
                            w.Write(chunk, 0, chunk.Length);
                        }
                        wavChunk = ms.ToArray(); // these bytes are now a proper .wav
                    }

                    // After creating wavChunk 
                    if (!IsSilent(chunk, _capture.WaveFormat))
                    {
                        // await Task.Run(() => AudioChunkReady?.Invoke(this, wavChunk));

                        List<string> transcriptList =  await _transcribe.DeepGramLiveTranscribe(wavChunk, _appointmentId);

                        string transcript = transcriptList.LastOrDefault() ?? "[No speech detected]";

                        if (!string.IsNullOrWhiteSpace(transcript))
                        {
                            _transcriptHistory.Add(transcript);
                            TranscriptReady?.Invoke(this, transcript);
                        }

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
       
            
        }

        public List<string> GetTranscriptHistory()
        {
            return _transcriptHistory.ToList(); // return a copy for safety
        }


        private bool IsSilent(byte[] buffer, WaveFormat format, double threshold = 0.01)
        {
            // assumes 16-bit PCM
            int bytesPerSample = format.BitsPerSample / 8;
            int samples = buffer.Length / bytesPerSample;
            if (samples == 0) return true;

            double sumSquares = 0;
            for (int i = 0; i < buffer.Length; i += bytesPerSample)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                double normalized = sample / 32768.0; // range -1 to 1
                sumSquares += normalized * normalized;
            }

            double rms = Math.Sqrt(sumSquares / samples);
            return rms < threshold; // true if "quiet enough"
        }


        private void Capture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // When recording stops, finalize and clean up
            _writer?.Dispose();
            _capture?.Dispose();

            Console.WriteLine("🛑 Recording stopped.");

            // Notify overlay that file is ready
            _ = GetEndOfMeetingTranscript(_tempFile, _appointmentId);
        }

        private async Task GetEndOfMeetingTranscript(string filePath, int appointmentId)
        {
            try
            {
                byte[] audioBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // 1️⃣ Full transcription
                // List<string> fullList = await _transcribe.DeepGramLiveTranscribe(audioBytes, appointmentId);
                //  string fullTranscript = string.Join(" ", fullList);

                FullMeetingTranscript = "Next interviewer. Okay. Hi. How are you? I'm ready for you if you just wanna come take a seat. My name is Midnight. It's really nice to meet you. I will be handling your interview today. Do you have a safe drive in? Everything okay? Fine. It's okay? Okay. Great. So today, I will be taking some notes throughout the interview just so you're aware. I do have some materials from the company. And then I also have my clipboard here with some specific questions that the director wanted me to ask you Of course, and then I have a little pamphlet here to just tell you a little bit more about us I was given these two folders. These are your materials your resume and cover letter cover letter, as well as your references. Correct? Okay. If you don't mind, I'm just gonna take a quick look, and then we'll get started. Okay. Perfect. So I've got everything I need here just so we can know. Okay. Perfect. And you are, interviewing today for the position of program manager with our company. Is that correct? You'd be surprised. A lot of people come in here and there's just been some sort of misunderstanding. So I always like to just verify. Okay. So I've got Google program manager, and we'll go ahead and just get started if that works for you. Okay, great. Okay, and first, could you just verify your name for me? And your date of birth? Okay. Perfect. So can we just start out, you telling me a little bit about yourself? I have your resume here, so I will just reference some of this as we go along. And I'm just gonna take a few notes. Okay. Okay. Alright. And you located nearby. Are you gonna relocate for the job? Okay. Okay. I see here that you started your career, essentially managing projects for large railway companies for the development of those projects. What were some of your greatest successes? Okay. And were you able to, implement any cost saving models at that time? What type of financial model were you following for those projects? Yeah. So on our online questionnaire, you had responded, that you had implemented in, several cost models for up to 20% project savings. Could you elaborate a little bit more on that statement? Okay. And how much time would you say it took to complete those? Okay. So I have you here. You have, quite a bit of management experience, it looks like. Yeah. Okay. That's wonderful. And here, I have that you have managed up to a 100 employees on each individual of the team. How many total employees have you managed at once, including all of your teams combined at any given time? Oh, wow. Okay. And how many direct reports did you have at that time? Okay. That's a lot. And what position were you holding at that time? Let's see here. You are? Okay. Okay. So that was when you were a director from 2017 to 2021? Mhmm. Oh, okay. Sure. And why did you leave that position? Downsizing of the company during COVID. Okay. And then at that point, it looks like you took a break and you did just private contract. Yeah. You did private contract consultations. You worked with a specific company. So that was 2121 to late twenty twenty two. Okay. And during that time, how many projects did you, provide consultations for roughly? Okay. Okay. And you were just doing, hourly consultation rates or project totals. Okay. And were you providing, like, were you implementing cost models for them as well, or were you just giving them the consultation services and different information like that? Okay. And do you have any project management certifications? Belt? Sure. And you have point three masters degree. Correct? In project management, looks like. Uh-huh. And business administration as well. So two master's degrees then. Uh-huh. Okay. And you're currently employed with them? And you've been with them for about a year and two months. Is there a reason that you want to leave? Is there okay. And how long have you been experiencing those difficulties? Okay. And what type of leadership style would you say that you have? Okay. And typically, when you have a situation like that, what would how would you deal with that directly with your with your direct reports? Mhmm. Absolutely. Okay. And have you experienced those types of challenges in other positions that you've held, or is this a fairly new, difficulty that you're facing? Okay. Alright. So you're having trouble kind of focusing with that project. Is that a different type of project for you? Oh, okay. So what what is your typical type of project that you're used to managing? Industrial development. Okay. What else? Okay. Oh, wow. Okay. And you so you've, overseen the development of a number of hospitals. Okay. And medical facilities in general. Okay. Now we have actually quite a few of those, so that's a great, plus. We are looking for someone with that type of background. How many hospitals? Three. And which hospitals were those? Okay. Okay. Were you located in California at that time? Okay. So you did you have done, virtual project management to a degree. Yes. And how often were you flying back? So you did, on-site inspections at thirty, sixty, 90 of the progress. Okay. So I have a couple of things I wanna show you. So here are a couple of the current diagrams for our developers that we're working on. Okay. So we have my pen here to show you. We have these four and then these three. These are the ones that we have that are the most profitable for us. So we're trying to push those. We also have a competitor who develops based on this model. And so what our goal is is to not just encourage these four but also market them openly on the sites that we're already developing on so I'll just give you a second to take a look at those before you kind of browse through there's a couple other options. Where's the other one? Yeah. So I just want to take a look at how funny. Give me your first thoughts. So what you think are, maybe some of the challenges to, selling these against one of these, and give me any recommendations that you have. Okay? So I'll just set that there with you for now. It'll give me just a second to take a look. Okay? Okay. Yeah, sure. Whenever you're ready. I'm just gonna take a few notes. Okay. So you're referencing here, you would open this a little bit more as opposed to this feeling more closed. To the point, I see it. Okay. What else? Okay. And you when you say drive down cost, what would some of your recommendations be to do that? Okay. So high quality but cheaper alternatives. Can you get some, feedback on that? Okay. So like you said, replacing hardwood with LVP. Anything else? Okay. So doing some quartz in place with marble. Okay. Great. And, between the two, did you see any other major concerns that you would have if you were doing project management for this project? This one and this one. Okay. Okay. I'm gonna show you a couple of photos next. So we are looking at the potential future development of a number of areas that we have not dived into yet. Just gonna go back to the beginning and show you. So we're looking at some of our rural community, projects. So here is one of them, when we're looking at doing a glamping development site, here in the Smoky Mountains. Some pictures of that. And,";

                Console.WriteLine("✔ Final summary generated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error generating final summary: " + ex.Message);
            }

        }

        public string GetFullMeetingTranscript()
        {
            FullMeetingTranscript = "Next interviewer. Okay. Hi. How are you? I'm ready for you if you just wanna come take a seat. My name is Midnight. It's really nice to meet you. I will be handling your interview today. Do you have a safe drive in? Everything okay? Fine. It's okay? Okay. Great. So today, I will be taking some notes throughout the interview just so you're aware. I do have some materials from the company. And then I also have my clipboard here with some specific questions that the director wanted me to ask you Of course, and then I have a little pamphlet here to just tell you a little bit more about us I was given these two folders. These are your materials your resume and cover letter cover letter, as well as your references. Correct? Okay. If you don't mind, I'm just gonna take a quick look, and then we'll get started. Okay. Perfect. So I've got everything I need here just so we can know. Okay. Perfect. And you are, interviewing today for the position of program manager with our company. Is that correct? You'd be surprised. A lot of people come in here and there's just been some sort of misunderstanding. So I always like to just verify. Okay. So I've got Google program manager, and we'll go ahead and just get started if that works for you. Okay, great. Okay, and first, could you just verify your name for me? And your date of birth? Okay. Perfect. So can we just start out, you telling me a little bit about yourself? I have your resume here, so I will just reference some of this as we go along. And I'm just gonna take a few notes. Okay. Okay. Alright. And you located nearby. Are you gonna relocate for the job? Okay. Okay. I see here that you started your career, essentially managing projects for large railway companies for the development of those projects. What were some of your greatest successes? Okay. And were you able to, implement any cost saving models at that time? What type of financial model were you following for those projects? Yeah. So on our online questionnaire, you had responded, that you had implemented in, several cost models for up to 20% project savings. Could you elaborate a little bit more on that statement? Okay. And how much time would you say it took to complete those? Okay. So I have you here. You have, quite a bit of management experience, it looks like. Yeah. Okay. That's wonderful. And here, I have that you have managed up to a 100 employees on each individual of the team. How many total employees have you managed at once, including all of your teams combined at any given time? Oh, wow. Okay. And how many direct reports did you have at that time? Okay. That's a lot. And what position were you holding at that time? Let's see here. You are? Okay. Okay. So that was when you were a director from 2017 to 2021? Mhmm. Oh, okay. Sure. And why did you leave that position? Downsizing of the company during COVID. Okay. And then at that point, it looks like you took a break and you did just private contract. Yeah. You did private contract consultations. You worked with a specific company. So that was 2121 to late twenty twenty two. Okay. And during that time, how many projects did you, provide consultations for roughly? Okay. Okay. And you were just doing, hourly consultation rates or project totals. Okay. And were you providing, like, were you implementing cost models for them as well, or were you just giving them the consultation services and different information like that? Okay. And do you have any project management certifications? Belt? Sure. And you have point three masters degree. Correct? In project management, looks like. Uh-huh. And business administration as well. So two master's degrees then. Uh-huh. Okay. And you're currently employed with them? And you've been with them for about a year and two months. Is there a reason that you want to leave? Is there okay. And how long have you been experiencing those difficulties? Okay. And what type of leadership style would you say that you have? Okay. And typically, when you have a situation like that, what would how would you deal with that directly with your with your direct reports? Mhmm. Absolutely. Okay. And have you experienced those types of challenges in other positions that you've held, or is this a fairly new, difficulty that you're facing? Okay. Alright. So you're having trouble kind of focusing with that project. Is that a different type of project for you? Oh, okay. So what what is your typical type of project that you're used to managing? Industrial development. Okay. What else? Okay. Oh, wow. Okay. And you so you've, overseen the development of a number of hospitals. Okay. And medical facilities in general. Okay. Now we have actually quite a few of those, so that's a great, plus. We are looking for someone with that type of background. How many hospitals? Three. And which hospitals were those? Okay. Okay. Were you located in California at that time? Okay. So you did you have done, virtual project management to a degree. Yes. And how often were you flying back? So you did, on-site inspections at thirty, sixty, 90 of the progress. Okay. So I have a couple of things I wanna show you. So here are a couple of the current diagrams for our developers that we're working on. Okay. So we have my pen here to show you. We have these four and then these three. These are the ones that we have that are the most profitable for us. So we're trying to push those. We also have a competitor who develops based on this model. And so what our goal is is to not just encourage these four but also market them openly on the sites that we're already developing on so I'll just give you a second to take a look at those before you kind of browse through there's a couple other options. Where's the other one? Yeah. So I just want to take a look at how funny. Give me your first thoughts. So what you think are, maybe some of the challenges to, selling these against one of these, and give me any recommendations that you have. Okay? So I'll just set that there with you for now. It'll give me just a second to take a look. Okay? Okay. Yeah, sure. Whenever you're ready. I'm just gonna take a few notes. Okay. So you're referencing here, you would open this a little bit more as opposed to this feeling more closed. To the point, I see it. Okay. What else? Okay. And you when you say drive down cost, what would some of your recommendations be to do that? Okay. So high quality but cheaper alternatives. Can you get some, feedback on that? Okay. So like you said, replacing hardwood with LVP. Anything else? Okay. So doing some quartz in place with marble. Okay. Great. And, between the two, did you see any other major concerns that you would have if you were doing project management for this project? This one and this one. Okay. Okay. I'm gonna show you a couple of photos next. So we are looking at the potential future development of a number of areas that we have not dived into yet. Just gonna go back to the beginning and show you. So we're looking at some of our rural community, projects. So here is one of them, when we're looking at doing a glamping development site, here in the Smoky Mountains. Some pictures of that. And,";

            return FullMeetingTranscript;

        }
        public void StopListening(int appointnmentid)
        {
            _capture?.StopRecording();
            Console.WriteLine("Listening stopped. File saved.");
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _capture?.Dispose();
        }
    }
}
