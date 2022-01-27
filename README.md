# BlendoBot.Module.WheelOfFortune
## Play a round of the Second Guess puzzle
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/BlendoBot/BlendoBot.Module.WheelOfFortune/Tests)

A fun game where all users in a Discord channel try to guess a revealing puzzle first.

## Discord Usage
- `?wof`
  - Starts a new game of Wheel of Fortune.

In a game of Wheel of Fortune, a puzzle is presented showing a category and the lengths of the words that need to be guessed like below:
```
Event

˷˷˷˷˷˷˷˷ ˷˷˷˷˷˷˷˷˷˷
```
Over 30 seconds, random letters will reveal themselves. The game is a competition to see who can correctly type the phrase in the channel first. All messages in that channel after the game starts are assumed to be participating in the game. If the response is incorrect, an ❌ will be reacted to their message, and every future message they type will be ignored. If the response is correct, the game ends and the puzzle will be updated to say that user won.

Phrases can include spaces and other characters. The only detection mechanism is whether the letters (case-insensitive) are in the correct order. This means a puzzle like `GREAT-FROG LEGS` can be correctly answered with `greatfroglegs`.

**NOTE:** If a puzzle includes the character `&`, you cannot type `AND` instead; please either ignore the character or use `&`.

## Config
Puzzles are loaded from a `puzzles.txt` in this module's common directory (e.g. `data/common/com.biendeo.blendobot.module.wheeloffortune/`). The `puzzles.txt` file should consist of a semi-colon separated list of categories and puzzles. An example would be:
```
Around the House;Accent Cabinet
Food and Drink;Sour Patch Kids
People;Network Of Friends
```