// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Classification;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    [Export(typeof(ISignatureHelpSourceProvider))]
    [Name("TypedMessage SignatureHelp Provider")]
    [ContentType(TypeConstants.Content)]
    [Order(Before = "default")]
    internal class SignatureHelpSourceProvider : ISignatureHelpSourceProvider
    {
        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            return new SignatureHelpSource(this, textBuffer);
        }
    }


    internal class SignatureHelpSource : ISignatureHelpSource
    {
        private SignatureHelpSourceProvider provider;
        private ITextBuffer textBuffer;
        private IClassifier classifier;
        List<ClassificationSpan> replacementSpans;
        int replacementSpansVersion = -1;
        SnapshotPoint replacementSpansPoint;

        public SignatureHelpSource(SignatureHelpSourceProvider provider, ITextBuffer textBuffer)
        {
            this.provider = provider;
            this.textBuffer = textBuffer;
            this.classifier = this.provider.ClassifierAggregatorService.GetClassifier(this.textBuffer);
        }

        #region ISignatureHelpSource Members

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            // Find the span we're in...
            List<ClassificationSpan> replacementSpans = this.GetReplacementSpans(session);

            if (replacementSpans == null || replacementSpans.Count == 0)
            {
                return;
            }

            // If we're over a replacement block, expand the span to include the entire replacement...
            ITrackingSpan applicableToSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(
                new SnapshotSpan(
                    replacementSpans.First().Span.Start,
                    replacementSpans.Last().Span.End),
                SpanTrackingMode.EdgeInclusive);

            foreach (var signatureData in ReplacementSignature.AllSignatureData)
            {
                signatures.Add(new ReplacementSignature(
                    this,
                    session,
                    this.textBuffer,
                    applicableToSpan,
                    signatureData));
            }
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            if (session.Signatures == null || session.Signatures.Count == 0)
            {
                return null;
            }

            List<ClassificationSpan> replacementSpans = this.GetReplacementSpans(session);

            if (replacementSpans == null || replacementSpans.Count == 0)
            {
                return null;
            }

            ////ITrackingSpan applicableToSpan = completionSession.Signatures[0].ApplicableToSpan;
            // TODO: get the classifications for the span, and determine the best match...
            ////string text = applicableToSpan.GetText(applicableToSpan.TextBuffer.CurrentSnapshot);
            ClassificationSpan typeSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementType));
            ClassificationSpan positionSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementPosition));
            ClassificationSpan nameSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementName));
            ClassificationSpan alignmentSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementAlignment));
            ClassificationSpan formatSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementFormat));

            bool hasType = typeSpan != null;
            bool hasPosition = positionSpan != null;
            bool hasAlignment = alignmentSpan != null;
            bool hasFormat = formatSpan != null;

            // Even if some of the optional values aren't present, the delimiters around them are enough to clue us in...
            if (!hasType && !hasPosition &&
                replacementSpans.Skip(1).Take(1).Any(s => "[" == s.Span.GetText()))
            {
                hasType = true;
            }
            else if (hasType && !hasPosition &&
                replacementSpans.ElementsAfter(typeSpan).Take(1).Any(s => "," == s.Span.GetText()))
            {
                hasPosition = true;
            }

            if (!hasAlignment &&
                replacementSpans.ElementsAfter(nameSpan).Any(s => "," == s.Span.GetText()))
            {
                hasAlignment = true;
            }

            if (!hasFormat &&
                replacementSpans.ElementsAfter(nameSpan).Any(s => ":" == s.Span.GetText()))
            {
                hasFormat = true;
            }

            int signatureKey = ReplacementSignature.CalculateSignatureKey(hasType, hasPosition, hasAlignment, hasFormat);

            ISignature match = session.Signatures.FirstOrDefault(s =>
                {
                    if (s is ReplacementSignature)
                    {
                        return ((ReplacementSignature)s).SignatureKey == signatureKey;
                    }

                    return false;
                });

            return match;
        }

        #endregion

        private static Tuple<string, string> SignatureInfo(string signature, string documentation)
        {
            return new Tuple<string, string>(signature, documentation);
        }

        public List<ClassificationSpan> GetReplacementSpans(ISignatureHelpSession session)
        {
            ITextSnapshot snapshot = this.textBuffer.CurrentSnapshot;
            int currentVersion = snapshot.Version.VersionNumber;
            SnapshotPoint currentPoint = session.GetTriggerPoint(snapshot).Value;

            if (replacementSpansVersion != currentVersion || replacementSpansPoint != currentPoint)
            {
                // TODO: detect when the current point is within the spans and re-use?
                this.replacementSpans = this.GetReplacementSpans(currentPoint);
                this.replacementSpansVersion = currentVersion;
                this.replacementSpansPoint = currentPoint;
            }

            return this.replacementSpans;
        }

        // TODO: track the snapshot version so we don't re-calculate for each signature?
        private List<ClassificationSpan> GetReplacementSpans(SnapshotPoint point)
        {
            IList<ClassificationSpan> spans = this.classifier.GetClassificationSpans(point.GetContainingLine().Extent);
            ClassificationSpan span = spans.FirstOrDefault(s => s.Span.Contains(point - 1));

            if (span == null || !IsReplacementSpan(span))
            {
                return null;
            }

            // expand to find entire replacement block...
            int initialSpan = spans.IndexOf(span);
            int firstSpan = initialSpan;
            int lastSpan = initialSpan;

            while (firstSpan > 0 && !spans[firstSpan].ClassificationType.IsOfType(TypeConstants.ReplacementStart))
            {
                --firstSpan;
            }

            while (lastSpan < spans.Count - 1 && !spans[lastSpan].ClassificationType.IsOfType(TypeConstants.ReplacementEnd))
            {
                ++lastSpan;
            }

            return spans.Skip(firstSpan).Take(lastSpan - firstSpan + 1).ToList();
        }

        #region IDisposable Members

        private bool disposed;
        public void Dispose()
        {
            if (!this.disposed)
            {
                GC.SuppressFinalize(this);
                this.disposed = true;
            }
        }

        #endregion

        private static bool IsReplacementSpan(ClassificationSpan span)
        {
            return span.ClassificationType.IsOfType(TypeConstants.ReplacementStart) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementEnd) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementDelimiter) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementName) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementType) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementPosition) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementAlignment) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementFormat);
        }
    }

    internal class ReplacementSignature : ISignature
    {
        private SignatureHelpSource source;
        private ISignatureHelpSession session;
        private ITextBuffer subjectBuffer;
        private IParameter currentParameter;

        ////public ReplacementSignature(SignatureHelpSource source, ISignatureHelpSession session, ITextBuffer subjectBuffer, ITrackingSpan applicableToSpan, string replacementSignature, string documentation)
        public ReplacementSignature(SignatureHelpSource source, ISignatureHelpSession session, ITextBuffer subjectBuffer, ITrackingSpan applicableToSpan, SignatureData data)

        {
            this.source = source;
            this.session = session;
            this.subjectBuffer = subjectBuffer;
            this.ApplicableToSpan = applicableToSpan;
            this.Content = data.Name;
            this.Documentation = data.Documentation;
            this.SignatureKey = data.SignatureKey;

            List<IParameter> parameters = new List<IParameter>();
            foreach (var parameter in data.Parameters)
            {
                parameters.Add(new ReplacementParameter(parameter.Documentation, parameter.Locus, parameter.Name, this, parameter.SignatureKey));
            }

            this.Parameters = new ReadOnlyCollection<IParameter>(parameters);

            this.ComputeCurrentParameter();

            this.subjectBuffer.Changed += SubjectBuffer_Changed;
            this.session.Dismissed += Session_Dismissed;
        }

        private static Tuple<string, string>[] GetAllSignatures()
        {
            Tuple<string,string>[] allSignatures = new Tuple<string, string>[]
            {
                SignatureInfo(
                    "replacement {name}",
                    "Specifies a named string replacement parameter."),
                SignatureInfo(
                    "replacement {name:format}",
                    "Specifies a named string replacement parameter with a specific format."),
                SignatureInfo(
                    "replacement {name,alignment}",
                    "Specifies a named string replacement parameter with a specific alignment."),
                SignatureInfo(
                    "replacement {name,alignment:format}",
                    "Specifies a named string replacement parameter with a specific alignment and format."),

                SignatureInfo(
                    "replacement {[type]name}",
                    "Specifies a named, typed replacement parameter."),
                SignatureInfo(
                    "replacement {[type]name:format}",
                    "Specifies a named, typed replacement parameter with a specific format."),
                SignatureInfo(
                    "replacement {[type]name,alignment}",
                    "Specifies a named, typed replacement parameter with a specific alignment."),
                SignatureInfo(
                    "replacement {[type]name,alignment:format}",
                    "Specifies a named, typed replacement parameter with a specific alignment and format."),

                SignatureInfo(
                    "replacement {[position]name}",
                    "Specifies a named, positioned, string replacement parameter."),
                SignatureInfo(
                    "replacement {[position]name:format}",
                    "Specifies a named, positioned, string replacement parameter with a specific format."),
                SignatureInfo(
                    "replacement {[position]name,alignment}",
                    "Specifies a named, positioned, string replacement parameter with a specific alignment."),
                SignatureInfo(
                    "replacement {[position]name,alignment:format}",
                    "Specifies a named, positioned, string replacement parameter with a specific alignment and format."),
                
                SignatureInfo(
                    "replacement {[type,position]name}",
                    "Specifies a named, typed, positioned replacement parameter."),
                SignatureInfo(
                    "replacement {[type,position]name:format}",
                    "Specifies a named, typed, positioned replacement parameter with a specific format."),
                SignatureInfo(
                    "replacement {[type,position]name,alignment}",
                    "Specifies a named, typed, positioned replacement parameter with a specific alignment."),
                SignatureInfo(
                    "replacement {[type,position]name,alignment:format}",
                    "Specifies a named, typed, positioned replacement parameter with a specific alignment and format."),
            };

            return allSignatures;
        }

        private static Tuple<string,string> SignatureInfo(string signature, string description)
        {
            return new Tuple<string, string>(signature, description);
        }

        private static Tuple<string, int, string>[] GetAllParameters()
        {
            Tuple<string, int, string>[] allParameters = new Tuple<string, int, string>[]
            {
                ParamInfo("type", 0x8, "Type of the parameter in the generated code."),
                ParamInfo("position", 0x4, "Argument position for the parameter in the generated code."),
                ParamInfo("name", 0, "Name of the parameter in the generated code."),
                ParamInfo("alignment", 0x2, "Composite formatting alignment for the replacement."),
                ParamInfo("format", 0x1, "Composite formatting format for the replacement."),
            };

            return allParameters;
        }

        private static Tuple<string, int, string> ParamInfo(string name, int signatureKey, string documentation)
        {
            return new Tuple<string, int, string>(name, signatureKey, documentation);
        }

        internal static ReadOnlyCollection<SignatureData> AllSignatureData { get { return allSignatureData.Value; } }

        private static Lazy<ReadOnlyCollection<SignatureData>> allSignatureData = new Lazy<ReadOnlyCollection<SignatureData>>(
            () => new ReadOnlyCollection<SignatureData>(CreateAllSignatureData()));

        private static List<SignatureData> CreateAllSignatureData()
        {
            var allData = new List<SignatureData>();

            var allSignatures = GetAllSignatures();
            var allParameters = GetAllParameters();

            List<IParameter> parameters = new List<IParameter>();

            foreach (var signature in allSignatures)
            {
                var paramData = new List<ParameterData>();
                int signatureKey = 0;

                int locusSearchStart = 0;
                foreach (var parameter in allParameters)
                {
                    int locusStart = signature.Item1.IndexOf(parameter.Item1, locusSearchStart);
                    if (locusStart >= 0)
                    {
                        signatureKey |= parameter.Item2;
                        Span locus = new Span(locusStart, parameter.Item1.Length);
                        locusSearchStart = locusStart + parameter.Item1.Length;

                        paramData.Add(new ParameterData(parameter.Item1, parameter.Item3, parameter.Item2, locus));
                    }
                }

                allData.Add(new SignatureData(signature.Item1, signature.Item2, signatureKey, paramData));
            }

            return allData;
        }

        // We can't just use ISignature and IParameter classes for this, as they get tied to a particular session...
        internal class SignatureData
        {
            public SignatureData(string name, string documentation, int signatureKey, IList<ParameterData> parameters)
            {
                this.Name = name;
                this.Documentation = documentation;
                this.SignatureKey = signatureKey;
                this.Parameters = new ReadOnlyCollection<ParameterData>(parameters);
            }

            public string Name { get; private set; }
            public string Documentation { get; private set; }
            public int SignatureKey { get; private set; }
            public ReadOnlyCollection<ParameterData> Parameters { get; private set; }
        }

        internal class ParameterData
        {
            public ParameterData(string name, string documentation, int signatureKey, Span locus)
            {
                this.Name = name;
                this.Documentation = documentation;
                this.SignatureKey = signatureKey;
                this.Locus = locus;
            }

            public string Name { get; private set; }
            public string Documentation { get; private set; }
            public int SignatureKey { get; private set; }
            public Span Locus { get; private set; }
        }

        // A convenient way to check/find signatures.
        public static int CalculateSignatureKey(bool hasType, bool hasPosition, bool hasAlignment, bool hasFormat)
        {
            return (hasType ? 0x8 : 0) | (hasPosition ? 0x4 : 0) | (hasAlignment ? 0x2 : 0) | (hasFormat ? 0x1 : 0);
        }

        public int SignatureKey { get; private set; }

        #region ISignature Members

        public ITrackingSpan ApplicableToSpan { get; private set; }
        public string Content { get; private set; }
        public string Documentation { get; private set; }
        public ReadOnlyCollection<IParameter> Parameters { get; private set; }
        public string PrettyPrintedContent { get; private set; }

        public IParameter CurrentParameter
        {
            get
            {
                return this.currentParameter;
            }

            private set
            {
                if (value != this.currentParameter)
                {
                    IParameter previous = this.currentParameter;
                    this.currentParameter = value;
                    this.FireCurrentParameterChanged(previous, value);
                }
            }
        }

        public event EventHandler<CurrentParameterChangedEventArgs>  CurrentParameterChanged;

        #endregion

        void SubjectBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            this.ComputeCurrentParameter();
        }

        void Session_Dismissed(object sender, EventArgs e)
        {
            this.subjectBuffer.Changed -= SubjectBuffer_Changed;
            this.session.Dismissed += Session_Dismissed;
        }

        private void FireCurrentParameterChanged(IParameter previousParameter, IParameter newParameter)
        {
            EventHandler<CurrentParameterChangedEventArgs> handler = this.CurrentParameterChanged;

            if (handler != null)
            {
                handler(this, new CurrentParameterChangedEventArgs(previousParameter, newParameter));
            }
        }

        public void ComputeCurrentParameter()
        {
            SnapshotPoint point = this.session.GetTriggerPoint(this.subjectBuffer.CurrentSnapshot).Value;
            this.ComputeCurrentParameter(point);
        }

        public void ComputeCurrentParameter(SnapshotPoint point)
        {
            if (this.Parameters.Count == 0)
            {
                this.CurrentParameter = null;
                return;
            }

            List<ClassificationSpan> replacementSpans = this.source.GetReplacementSpans(this.session);

            if (replacementSpans == null || replacementSpans.Count == 0)
            {
                this.CurrentParameter = null;
                return;
            }

            ClassificationSpan span = replacementSpans.FirstOrDefault(s => s.Span.Contains(point - 1));

            if (span == null)
            {
                this.CurrentParameter = null;
                return;
            }

            ClassificationSpan nameSpan = replacementSpans.FirstOrDefault(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementName));

            bool inType = false;
            bool inPosition = false;
            bool inAlignment = false;
            bool inFormat = false;

            if (span.ClassificationType.IsOfType(TypeConstants.ReplacementType) ||
                "[" == span.Span.GetText() && !replacementSpans.ElementsAfter(nameSpan).Contains(span))
            {
                inType = true;
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.ReplacementPosition) ||
                "," == span.Span.GetText() && !replacementSpans.ElementsAfter(nameSpan).Contains(span))
            {
                inPosition = true;
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.ReplacementAlignment) ||
                "," == span.Span.GetText() && replacementSpans.ElementsAfter(nameSpan).Contains(span))
            {
                inAlignment = true;
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.ReplacementFormat) ||
                ":" == span.Span.GetText() && replacementSpans.ElementsAfter(nameSpan).Contains(span))
            {
                inFormat = true;
            }

            int signatureKey = ReplacementSignature.CalculateSignatureKey(inType, inPosition, inAlignment, inFormat);

            IParameter match = this.Parameters.FirstOrDefault(p =>
            {
                if (p is ReplacementParameter)
                {
                    return ((ReplacementParameter)p).SignatureKey == signatureKey;
                }

                return false;
            });

            this.CurrentParameter = match;
        }
    }

    internal class ReplacementParameter : IParameter
    {
        public ReplacementParameter(string documentation, Span locus, string name, ISignature signature, int signatureKey)
        {
            this.Documentation = documentation;
            this.Locus = locus;
            this.Name = name;
            this.Signature = signature;
            this.SignatureKey = signatureKey;
        }

        public int SignatureKey { get; private set; }

        #region IParameter Members

        public string Documentation { get; private set; }
        public Span Locus { get; private set; }
        public string Name { get; private set; }
        public Span PrettyPrintedLocus { get; private set; }
        public ISignature Signature { get; private set; }

        #endregion
    }

    internal static class CustomLinq
    {
        ////public static IEnumerable<TSource> SafeWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        ////{
        ////    if (source != null)
        ////    {
        ////        return source.Where(predicate);
        ////    }

        ////    return null;
        ////}

        public static IEnumerable<TSource> ElementsAfter<TSource>(this IList<TSource> source, TSource element)
        {
            int index = source.IndexOf(element);
            return source.Skip(index + 1);
        }

    }
}
