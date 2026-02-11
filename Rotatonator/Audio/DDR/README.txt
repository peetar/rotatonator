# Place your DDR mode audio files here
# See DDR_AUDIO_FILES.md in the project root for the list of required files

DDR Mode has two audio modes:
1. Normal Mode (default): Uses simple, professional audio cues
2. Silly Mode (optional checkbox): Uses fun music and sound effects

SILLY MODE FILES (only play when "DDR Silly Mode" is checked):
Required folders and files:
- good_common/: Common positive feedback sounds (e.g., "perfect.mp3", "great.mp3")
- good_rare/: Rare positive sounds that play on long streaks (e.g., "on-fire.mp3", "high-score.mp3")
- bad_common/: Common negative feedback sounds (e.g., "groan.mp3", "early.mp3")
- bad_rare/: Rare negative sounds that play on bad streaks
- loops/: Background music tracks (randomly selected)

NORMAL MODE (future):
To add professional, non-silly audio cues, create these folders:
- normal/good/: Simple positive cues (e.g., beeps, chimes)
- normal/bad/: Simple negative cues (e.g., buzzer)
(Not yet implemented - currently uses no sound effects in normal mode)
