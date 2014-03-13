//-------------------------------------------------------------------------------------------------
// <copyright file="FindEntrySectionAndLoadSymbolsCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    internal class FindEntrySectionAndLoadSymbolsCommand
    {
        private IEnumerable<Section> sections;

        public FindEntrySectionAndLoadSymbolsCommand(IEnumerable<Section> sections)
        {
            this.sections = sections;
        }

        public OutputType ExpectedOutputType { get; set;}

        public Section EntrySection { get; private set; }

        public IDictionary<string, Symbol> Symbols { get; private set; }

        public IEnumerable<Symbol> PossiblyConflictingSymbols { get; private set; }

        /// <summary>
        /// Finds the entry section and loads the symbols from an array of intermediates.
        /// </summary>
        /// <param name="allowIdenticalRows">Flag specifying whether identical rows are allowed or not.</param>
        /// <param name="messageHandler">Message handler object to route all errors through.</param>
        /// <param name="expectedOutputType">Expected entry output type, based on output file extension provided to the linker.</param>
        /// <param name="entrySection">Located entry section.</param>
        /// <param name="allSymbols">Collection of symbols loaded.</param>
        public void Execute()
        {
            Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();
            HashSet<Symbol> possibleConflicts = new HashSet<Symbol>();

            SectionType expectedEntrySectionType;
            if (!Enum.TryParse<SectionType>(this.ExpectedOutputType.ToString(), out expectedEntrySectionType))
            {
                expectedEntrySectionType = SectionType.Unknown;
            }

            foreach (Section section in this.sections)
            {
                // Try to find the one and only entry section.
                if (SectionType.Product == section.Type || SectionType.Module == section.Type || SectionType.PatchCreation == section.Type || SectionType.Patch == section.Type || SectionType.Bundle == section.Type)
                {
                    if (SectionType.Unknown != expectedEntrySectionType && section.Type != expectedEntrySectionType)
                    {
                        string outputExtension = Output.GetExtension(this.ExpectedOutputType);
                        Messaging.Instance.OnMessage(WixWarnings.UnexpectedEntrySection(section.SourceLineNumbers, section.Type.ToString(), expectedEntrySectionType.ToString(), outputExtension));
                    }

                    if (null == this.EntrySection)
                    {
                        this.EntrySection = section;
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.MultipleEntrySections(this.EntrySection.SourceLineNumbers, this.EntrySection.Id, section.Id));
                        Messaging.Instance.OnMessage(WixErrors.MultipleEntrySections2(section.SourceLineNumbers));
                    }
                }

                // Load all the symbols from the section's tables that create symbols.
                foreach (Table table in section.Tables.Where(t => t.Definition.CreateSymbols))
                {
                    foreach (Row row in table.Rows)
                    {
                        Symbol symbol = new Symbol(row);

                        Symbol existingSymbol;
                        if (!symbols.TryGetValue(symbol.Name, out existingSymbol))
                        {
                            symbols.Add(symbol.Name, symbol);
                        }
                        else // uh-oh, duplicate symbols.
                        {
                            // If the duplicate symbols are both private directories, there is a chance that they
                            // point to identical rows. Identical directory rows are redundant and will not cause
                            // conflicts.
                            if (AccessModifier.Private == existingSymbol.Access && AccessModifier.Private == symbol.Access &&
                                "Directory" == existingSymbol.Row.Table.Name && existingSymbol.Row.IsIdentical(symbol.Row))
                            {
                                // Ensure identical symbol's row is marked redundant to ensure (should the row be
                                // referenced into the final output) it will not add duplicate primary keys during
                                // the .IDT importing.
                                symbol.Row.Redundant = true;
                                existingSymbol.AddRedundant(symbol);
                            }
                            else
                            {
                                existingSymbol.AddPossibleConflict(symbol);
                                possibleConflicts.Add(existingSymbol);
                            }
                        }
                    }
                }
            }

            this.Symbols = symbols;
            this.PossiblyConflictingSymbols = possibleConflicts;
        }
    }
}
