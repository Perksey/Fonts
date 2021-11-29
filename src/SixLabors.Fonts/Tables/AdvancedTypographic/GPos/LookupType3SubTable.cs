// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Cursive Attachment Positioning Subtable.
    /// Some cursive fonts are designed so that adjacent glyphs join when rendered with their default positioning.
    /// However, if positioning adjustments are needed to join the glyphs, a cursive attachment positioning (CursivePos) subtable can describe
    /// how to connect the glyphs by aligning two anchor points: the designated exit point of a glyph, and the designated entry point of the following glyph.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#cursive-attachment-positioning-format1-cursive-attachment"/>
    /// </summary>
    internal static class LookupType3SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort posFormat = reader.ReadUInt16();

            return posFormat switch
            {
                1 => LookupType3Format1SubTable.Load(reader, offset, lookupFlags),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'posFormat' {posFormat}. Should be '1'.")
            };
        }

        internal sealed class LookupType3Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly EntryExitAnchors[] entryExitAnchors;

            public LookupType3Format1SubTable(CoverageTable coverageTable, EntryExitAnchors[] entryExitAnchors, LookupFlags lookupFlags)
                : base(lookupFlags)
            {
                this.coverageTable = coverageTable;
                this.entryExitAnchors = entryExitAnchors;
            }

            public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags)
            {
                // Cursive Attachment Positioning Format1.
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Type               |  Name                           | Description                                          |
                // +====================+=================================+======================================================+
                // | uint16             | posFormat                       | Format identifier: format = 1                        |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | coverageOffset                  | Offset to Coverage table,                            |
                // |                    |                                 | from beginning of CursivePos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | uint16             | entryExitCount                  | Number of EntryExit records.                         |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | EntryExitRecord    | entryExitRecord[entryExitCount] | Array of EntryExit records, in Coverage index order. |
                // +--------------------+---------------------------------+------------------------------------------------------+
                ushort coverageOffset = reader.ReadOffset16();
                ushort entryExitCount = reader.ReadUInt16();
                var entryExitRecords = new EntryExitRecord[entryExitCount];
                for (int i = 0; i < entryExitCount; i++)
                {
                    entryExitRecords[i] = new EntryExitRecord(reader, offset);
                }

                var entryExitAnchors = new EntryExitAnchors[entryExitCount];
                for (int i = 0; i < entryExitCount; i++)
                {
                    entryExitAnchors[i] = new EntryExitAnchors(reader, offset, entryExitRecords[i]);
                }

                var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

                return new LookupType3Format1SubTable(coverageTable, entryExitAnchors, lookupFlags);
            }

            public override bool TryUpdatePosition(
                FontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                if (count <= 1)
                {
                    return false;
                }

                // Implements Cursive Attachment Positioning Subtable:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                ushort nextIndex = (ushort)(index + 1);
                ushort nextGlyphId = collection[nextIndex][0];
                if (nextGlyphId == 0)
                {
                    return false;
                }

                int coverageNext = this.coverageTable.CoverageIndexOf(nextGlyphId);
                if (coverageNext < 0)
                {
                    return false;
                }

                EntryExitAnchors nextRecord = this.entryExitAnchors[coverageNext];
                AnchorTable? entry = nextRecord.EntryAnchor;
                if (entry is null)
                {
                    return false;
                }

                int coverage = this.coverageTable.CoverageIndexOf(glyphId);
                if (coverage < 0)
                {
                    return false;
                }

                EntryExitAnchors curRecord = this.entryExitAnchors[coverage];
                AnchorTable? exit = curRecord.ExitAnchor;
                if (exit is null)
                {
                    return false;
                }

                GlyphShapingData current = collection.GetGlyphShapingData(index);
                GlyphShapingData next = collection.GetGlyphShapingData(nextIndex);

                // TODO: Vertical.
                if (current.Direction == TextDirection.LeftToRight)
                {
                    current.Bounds.Width = exit.XCoordinate + current.Bounds.X;

                    int delta = entry.XCoordinate + next.Bounds.X;
                    next.Bounds.Width -= delta;
                    next.Bounds.X -= delta;
                }
                else
                {
                    int delta = exit.XCoordinate + current.Bounds.X;
                    current.Bounds.Width -= delta;
                    current.Bounds.X -= delta;

                    next.Bounds.Width = entry.XCoordinate + next.Bounds.X;
                }

                int child = index;
                int parent = nextIndex;
                int xOffset = entry.XCoordinate - exit.XCoordinate;
                int yOffset = entry.YCoordinate - exit.YCoordinate;
                if (this.LookupFlags.HasFlag(LookupFlags.RightToLeft))
                {
                    int temp = child;
                    child = parent;
                    parent = temp;

                    xOffset = -xOffset;
                    yOffset = -yOffset;
                }

                // If child was already connected to someone else, walk through its old
                // chain and reverse the link direction, such that the whole tree of its
                // previous connection now attaches to new parent.Watch out for case
                // where new parent is on the path from old chain...
                bool horizontal = !collection.IsVerticalLayoutMode;
                ReverseCursiveMinorOffset(collection, index, child, horizontal, parent);

                GlyphShapingData c = collection.GetGlyphShapingData(child);
                c.CursiveAttachment = parent - child;
                if (horizontal)
                {
                    c.Bounds.Y = yOffset;
                }
                else
                {
                    c.Bounds.X = xOffset;
                }

                // If parent was attached to child, separate them.
                GlyphShapingData p = collection.GetGlyphShapingData(parent);
                if (p.CursiveAttachment == -c.CursiveAttachment)
                {
                    p.CursiveAttachment = 0;
                }

                return true;
            }

            private static void ReverseCursiveMinorOffset(
                GlyphPositioningCollection collection,
                int position,
                int i,
                bool horizontal,
                int parent)
            {
                GlyphShapingData c = collection.GetGlyphShapingData(i);
                int chain = c.CursiveAttachment;
                if (chain <= 0)
                {
                    return;
                }

                c.CursiveAttachment = 0;

                int j = i + chain;

                // Stop if we see new parent in the chain.
                if (j == parent)
                {
                    return;
                }

                ReverseCursiveMinorOffset(collection, position, j, horizontal, parent);

                GlyphShapingData p = collection.GetGlyphShapingData(j);
                if (horizontal)
                {
                    p.Bounds.Y = -c.Bounds.Y;
                }
                else
                {
                    p.Bounds.X = -c.Bounds.X;
                }

                p.CursiveAttachment = -chain;
            }
        }
    }
}