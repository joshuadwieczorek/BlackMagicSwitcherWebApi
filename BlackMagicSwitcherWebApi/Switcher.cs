using BMDSwitcherAPI;
using PusherServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace BlackMagicSwitcherWebApi
{
    public class Switcher
    {
        public delegate void SwitcherEventHandler(object sender, object args);
        private Pusher pusher;
        private MixEffectBlockMonitor m_mixEffectBlockMonitor;
        private IBMDSwitcherDiscovery m_switcherDiscovery;
        private IBMDSwitcher m_switcher;
        private SwitcherMonitor m_switcherMonitor;
        private List<InputMonitor> m_inputMonitors = new List<InputMonitor>();
        private IBMDSwitcherMixEffectBlock m_mixEffectBlock1;
        private List<StringObjectPair<long>> m_output_list = new List<StringObjectPair<long>>();

        public void OnSwitcherDisconnected(object sender, object args)
        {

        }


        public void OnSwitcherProgramChange(object sender, object args)
        {
            long programId;

            m_mixEffectBlock1.GetProgramInput(out programId);

            // Select the program popup entry that matches the input id:
            foreach (StringObjectPair<long> item in m_output_list)
            {
                if (item.value == programId)
                {
                    pusher.TriggerAsync(
                      "blackmagicswitcher",
                      "progam-switched",
                      new { camera = item.name, message = $"{item.name} is live" });
                    break;
                }
            }
        }


        public void OnSwitcherPreviewChange(object sender, object args)
        {
            long previewId;

            m_mixEffectBlock1.GetPreviewInput(out previewId);

            // Select the program popup entry that matches the input id:
            foreach (StringObjectPair<long> item in m_output_list)
            {
                if (item.value == previewId)
                {
                    pusher.TriggerAsync(
                      "blackmagicswitcher",
                      "preview-switched",
                      new { camera = item.name, message = $"{item.name} is prevewing" });
                    break;
                }
            }
        }


        public Switcher(string ipAddress)
        {

            var options = new PusherOptions
            {
                Cluster = "us2",
                Encrypted = true
            };

            pusher = new Pusher(
              "1137245",
              "f9cabd4c01730143d579",
              "3b87a4daf5b3f26ee1be",
              options);


            IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();
            _BMDSwitcherConnectToFailure failureReason;
            discovery.ConnectTo(ipAddress, out m_switcher, out failureReason);


            m_switcherMonitor = new SwitcherMonitor();
            m_switcherMonitor.SwitcherDisconnected += OnSwitcherDisconnected;

            m_mixEffectBlockMonitor = new MixEffectBlockMonitor();
            m_mixEffectBlockMonitor.ProgramInputChanged += OnSwitcherProgramChange;
            m_mixEffectBlockMonitor.PreviewInputChanged += OnSwitcherPreviewChange;

            m_switcher.AddCallback(m_switcherMonitor);

            IBMDSwitcherInput input = null;

            // We create input monitors for each input. To do this we iterate over all inputs:
            // This will allow us to update the combo boxes when input names change:
            IBMDSwitcherInputIterator inputIterator = null;
            IntPtr inputIteratorPtr;
            Guid inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator != null)
            {
                input = null;
                inputIterator.Next(out input);
                while (input != null)
                {
                    InputMonitor newInputMonitor = new InputMonitor(input);
                    input.AddCallback(newInputMonitor);

                    m_inputMonitors.Add(newInputMonitor);

                    inputIterator.Next(out input);
                }
            }


            //if (m_mixEffectBlock1 != null)
            //{
            //    // Remove callback
            //    m_mixEffectBlock1.RemoveCallback(m_mixEffectBlockMonitor);

            //    // Release reference
            //    m_mixEffectBlock1 = null;
            //}

            //if (m_switcher != null)
            //{
            //    // Remove callback:
            //    m_switcher.RemoveCallback(m_switcherMonitor);

            //    // release reference:
            //    m_switcher = null;
            //}


            // We want to get the first Mix Effect block (ME 1). We create a ME iterator,
            // and then get the first one:
            //m_mixEffectBlock1 = null;

            IBMDSwitcherMixEffectBlockIterator meIterator = null;
            IntPtr meIteratorPtr;
            Guid meIteratorIID = typeof(IBMDSwitcherMixEffectBlockIterator).GUID;
            m_switcher.CreateIterator(ref meIteratorIID, out meIteratorPtr);
            if (meIteratorPtr != null)
            {
                meIterator = (IBMDSwitcherMixEffectBlockIterator)Marshal.GetObjectForIUnknown(meIteratorPtr);
            }

            if (meIterator == null)
                return;

            if (meIterator != null)
            {
                meIterator.Next(out m_mixEffectBlock1);
            }


            // Install MixEffectBlockMonitor callbacks:
            m_mixEffectBlock1.AddCallback(m_mixEffectBlockMonitor);



            // Get an input iterator.
            inputIterator = null;
            inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator == null)
                return;

            input = null;
            inputIterator.Next(out input);
            while (input != null)
            {
                string inputName;
                long inputId;

                input.GetInputId(out inputId);
                input.GetLongName(out inputName);

                // Add items to list:
                m_output_list.Add(new StringObjectPair<long>(inputName, inputId));
                //m_output_list.Add(new StringObjectPair<long>(inputName, inputId));

                inputIterator.Next(out input);
            }



            //while (true)
            //{

            //}

        }


        class MixEffectBlockMonitor : IBMDSwitcherMixEffectBlockCallback
        {
            // Events:
            public event SwitcherEventHandler ProgramInputChanged;
            public event SwitcherEventHandler PreviewInputChanged;
            public event SwitcherEventHandler TransitionFramesRemainingChanged;
            public event SwitcherEventHandler TransitionPositionChanged;
            public event SwitcherEventHandler InTransitionChanged;

            public MixEffectBlockMonitor()
            {
            }

            void IBMDSwitcherMixEffectBlockCallback.Notify(_BMDSwitcherMixEffectBlockEventType eventType)
            {
                switch (eventType)
                {
                    case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeProgramInputChanged:
                        if (ProgramInputChanged != null)
                            ProgramInputChanged(this, null);
                        break;
                    case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypePreviewInputChanged:
                        if (PreviewInputChanged != null)
                            PreviewInputChanged(this, null);
                        break;
                    case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeTransitionFramesRemainingChanged:
                        if (TransitionFramesRemainingChanged != null)
                            TransitionFramesRemainingChanged(this, null);
                        break;
                    case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeTransitionPositionChanged:
                        if (TransitionPositionChanged != null)
                            TransitionPositionChanged(this, null);
                        break;
                    case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeInTransitionChanged:
                        if (InTransitionChanged != null)
                            InTransitionChanged(this, null);
                        break;
                }
            }

        }



        class SwitcherMonitor : IBMDSwitcherCallback
        {
            // Events:
            public event SwitcherEventHandler SwitcherDisconnected;

            public SwitcherMonitor()
            {
            }

            void IBMDSwitcherCallback.Notify(_BMDSwitcherEventType eventType, _BMDSwitcherVideoMode coreVideoMode)
            {
                if (eventType == _BMDSwitcherEventType.bmdSwitcherEventTypeDisconnected)
                {
                    if (SwitcherDisconnected != null)
                        SwitcherDisconnected(this, null);
                }
            }
        }


        class InputMonitor : IBMDSwitcherInputCallback
        {
            // Events:
            public event SwitcherEventHandler LongNameChanged;

            private IBMDSwitcherInput m_input;
            public IBMDSwitcherInput Input { get { return m_input; } }

            public InputMonitor(IBMDSwitcherInput input)
            {
                m_input = input;
            }

            void IBMDSwitcherInputCallback.Notify(_BMDSwitcherInputEventType eventType)
            {
                switch (eventType)
                {
                    case _BMDSwitcherInputEventType.bmdSwitcherInputEventTypeLongNameChanged:
                        if (LongNameChanged != null)
                            LongNameChanged(this, null);
                        break;
                }
            }
        }

        /// <summary>
        /// Used for putting other object types into combo boxes.
        /// </summary>
        struct StringObjectPair<T>
        {
            public string name;
            public T value;

            public StringObjectPair(string name, T value)
            {
                this.name = name;
                this.value = value;
            }

            public override string ToString()
            {
                return name;
            }
        }

    }
}