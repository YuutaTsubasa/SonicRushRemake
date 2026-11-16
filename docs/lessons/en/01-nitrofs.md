---
lesson: 1
lang: en
title: Reading the ROM's NitroFS File System
date: 2026-11-16
milestone: 1
tags: [rom, nitrofs, binary-parsing]
---

# Lesson 1: Reading the ROM's NitroFS File System

## Goal

Write a console program (no SDL yet) that takes the path to the US ROM, lists the full path and size of every file inside it, and prints the total count at the end.

This is the entry point for everything that follows: backgrounds (`.bbg`), animations (`.bac`) and level maps (`.narc`) are all read out of this file system.

## Background: what an NDS ROM looks like

An NDS cartridge ROM is one large file. It starts with a fixed-format **header**, followed by code (ARM9/ARM7), overlays, and a small read-only file system called **NitroFS**. NitroFS is made of two tables:

- **FAT** (File Allocation Table): where each file's data lives in the ROM. Positions only, no names.
- **FNT** (File Name Table): the directory structure and file names, mapping names to file IDs.

The two tables are linked by the **file ID**: the FNT tells you "`bg/sp_fix_base.bbg` is file N", and FAT entry N tells you "it lives here in the ROM".

All integers are **little-endian**.

## Format

### 1. Header (start of the ROM)

| Offset | Size | Contents |
| --- | --- | --- |
| `0x40` | u32 | FNT offset in the ROM |
| `0x44` | u32 | FNT size |
| `0x48` | u32 | FAT offset in the ROM |
| `0x4C` | u32 | FAT size |

### 2. FAT: 8 bytes per file

| Offset | Size | Contents |
| --- | --- | --- |
| +0 | u32 | Start (position in the ROM) |
| +4 | u32 | End (**exclusive**) |

Entry N is file ID N; its size is `end − start`.

### 3. FNT main table: 8 bytes per directory

Located at the start of the FNT. Entry i is directory `0xF000 + i`; the root is `0xF000` (entry 0).

| Offset | Size | Contents |
| --- | --- | --- |
| +0 | u32 | Sub-table offset (**relative to the start of the FNT**, not the ROM) |
| +4 | u16 | ID of the first file in this directory |
| +6 | u16 | Parent directory ID; **for the root this holds the total number of directories** |

### 4. FNT sub-table: a directory's contents

A sequence of entries, each one:

1. One byte:
   - `0x00`: end of this directory.
   - Otherwise the low 7 bits (`& 0x7F`) are the name length; if the `0x80` bit is set, the entry is a **directory**.
2. The name (ASCII, no terminating `0`).
3. For a directory only: a u16 with the subdirectory ID (`0xF000 + index`).

Files in the same directory are numbered starting from the main table's "first file ID", **incrementing by 1 for each file** (directories don't use up file IDs).

## C# hints

- Read the whole file with `File.ReadAllBytes` and slice it with `ReadOnlySpan<byte>`: `rom.Slice(fntOffset, fntSize)`.
- Read integers with `BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0x40))` and `ReadUInt16LittleEndian(...)` (`System.Buffers.Binary`).
- Walking the sub-tables **recursively** from the root is the most natural approach: when you hit a directory entry, recurse with its subdirectory ID and extend the path.
- `ReadOnlySpan<T>` is a `ref struct`: it can't be stored in a class field, captured in a lambda, or used in an `async` method or `yield` iterator. Either pass the span down through ordinary methods, or keep the `byte[]` and call `.AsSpan()` when you need it.
- Suggested location: the future `Rush.Core/Rom/`, e.g. a `NitroFs` class that exposes "the file list" and "get a file's contents by path or ID". Lesson 2 will need the latter.

## Self-check

- The US ROM should list **2245** files.
- The list must include: `narc/z11_map.narc`, `bg/sp_fix_base.bbg`, `ss/ss_1.sst`, `act/ac_spst.bac`.
- If your count is off, ask yourself whether "the number of FAT entries" and "the number of named files" are really the same thing.

<details>
<summary>Reference answer (look after you finish)</summary>

US ROM (game code `ASCE`):

| Item | Value |
| --- | --- |
| FNT | `0x1AF000`, size `0xA136` |
| FAT | `0x1B9200`, size `0x4638` → **2247 entries** |
| Directories | 62 |
| Root's first file ID | **2** |
| Named files | **2245** |

The FAT has 2 more entries than there are named files: IDs 0 and 1 are **overlays** (code the ARM9 loads on demand). They are referenced from a separate overlay table in the header and have no names in the FNT.

| File | ID | Size (bytes) |
| --- | --- | --- |
| `act/ac_spst.bac` | 203 | 65360 |
| `bg/sp_fix_base.bbg` | 997 | 5164 |
| `narc/z11_map.narc` | 1876 | 69132 |
| `ss/ss_1.sst` | 1999 | 6636 |

The first file is `act/ac_dec_algae.bac` (#2) and the last is `talk/talk_demo_zone_son7_spa.btk` (#2246).

</details>

## What I learned

<!-- Fill this in yourself after finishing: pitfalls, approaches, things to change later -->
