# DDR Mode Audio Files

Place these audio files in the `Audio/DDR/` folder next to the Rotatonator executable.

## Required Audio Files

### Other Healers Feedback
- `perfect.mp3` (or .wav) - Played when another healer casts exactly on time (within ±0.5 seconds)
- `groan.mp3` (or .wav) - Played when another healer is more than 1 second late

### Player Feedback - Early
- `early.mp3` (or .wav) - Played when player casts before their turn

### Player Feedback - Late
- `groan.mp3` (or .wav) - Played when player is more than 1 second late (reuses other healer groan)

### Player Feedback - Perfect Combo Escalation
Perfect casts in sequence trigger escalating encouragement:
1. `great.mp3` (or .wav) - First perfect cast
2. `wow.mp3` (or .wav) - Second perfect cast
3. `heating-up.mp3` (or .wav) - Third perfect cast
4. `perfect.mp3` (or .wav) - Fourth perfect cast
5. `perfect.mp3` (or .wav) - Fifth perfect cast
6. `on-fire.mp3` (or .wav) - Sixth perfect cast
7. `high-score.mp3` (or .wav) - Seventh perfect cast
8. `perfect.mp3` (or .wav) - All subsequent perfect casts until streak ends

## File Specifications
- Format: MP3 or WAV (both fully supported)
- Sample Rate: 44100 Hz recommended
- Channels: Mono or Stereo
- Bit Depth: 16-bit (for WAV)

## Notes
- All files should be short (0.5-2 seconds) for quick feedback
- Audio should be clear and distinct
- Consider using upbeat, game-show style sounds for positive feedback
- "Groan" should be a disappointed audience sound
- "You're heating up" and "You're on fire" are references to NBA Jam
