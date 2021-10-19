using CSharpSynth.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;


public class MusicBlockRepeat : MusicBlock
{
	private readonly MusicBlock[] m_children;
	private readonly uint[] m_schedule;


	public MusicBlockRepeat(MusicBlock[] children, uint[] schedule)
	{
		Assert.AreNotEqual(children.Length, 0);
		Assert.IsTrue(schedule.Length >= children.Length);
		m_children = children;
		m_schedule = schedule;
	}

	public override uint SixtyFourthsTotal() => CombineViaSchedule(block => new List<uint> { block.SixtyFourthsTotal() }).Aggregate((a, b) => a + b);

	public override List<NoteTimePair> GetNotes(uint timeOffset)
	{
		uint timeItr = timeOffset;
		return CombineViaSchedule(block => {
			List<NoteTimePair> notes = block.GetNotes(timeItr);
			timeItr += block.SixtyFourthsTotal();
			return notes;
		});
	}

	public override List<MidiEvent> ToMidiEvents(uint startSixtyFourths, uint rootKey, MusicScale scale, uint samplesPerSixtyFourth, uint channelIdx)
	{
		uint sixtyFourthsItr = startSixtyFourths;
		return CombineViaSchedule(block => {
			List<MidiEvent> list = block.ToMidiEvents(sixtyFourthsItr, rootKey, scale, samplesPerSixtyFourth, channelIdx);
			sixtyFourthsItr += block.SixtyFourthsTotal();
			return list;
		});
	}

	public override MusicBlock SplitNotes() => new MusicBlockRepeat(m_children.Select(block => block.SplitNotes()).ToArray(), m_schedule);

	public override MusicBlock MergeNotes() => new MusicBlockRepeat(m_children.Select(block => block.MergeNotes()).ToArray(), m_schedule);


	private List<T> CombineViaSchedule<T>(Func<MusicBlock, List<T>> blockFunc)
	{
		List<T> list = new List<T>();
		foreach (uint index in m_schedule)
		{
			MusicBlock childBlock = m_children[index];
			list.AddRange(blockFunc(childBlock));
		}
		return list;
	}
}
