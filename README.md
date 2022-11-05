# Keyrita Beta 0.7.1
Keyrita is a user-friendly keyboard layout analyzer and generator. No more editing your layouts in notepads and waiting for stats. Now you can Swap keys, and instantly view the results. You can undo/redo, save/load, copy/paste, and much more all with the click of a button! Keyrita is also designed to be fine-tuned to allow users to express their preferences in an easy-to-understand way all so they can invent the best keyboard layout for them.

## Build
First, make sure you have installed Dotnet 6. The project will not build with anything older.
Next, it's highly recommended to build using Visual Studio.
Clone the repository, change the configuration to release, then build and run.

## Edit Modes
The app is designed to support multiple types of waves the user can interact with it. Two modes have been implemented so far, but many will come in the future.

### Normal mode
This is the main way users interact with the software, and when they start a new project, this is the mode they will start in. Keys can be selected by typing their letter on the keyboard or by double clicking the key. This is useful to quickly see where a key is positioned so you don't have to spend time scanning the keyboard for its location. It also modifies which version of the heatmap you see (See Heatmaps). Keys can also be click and dragged on top of each other to swap their positions. Finally the positions of certain keys can be locked by shift clicking on them. After doing so, an lock icon will appear on the key indicating it's locked state.

### Scissor map mode
Currently this feature only implements a view of the scissor map. The map will be modifiable through click and dragging.

### Effort map mode
This feature exists in the UI, but doesn't do anything yet.

## Datasets
Datasets are the core set of parameters for analysis. They contain the frequencies of each character in the alphabet along with bigram, trigram, and skipgram frequencies. By default, a dataset will not be loaded, but you can load one from the menu under file->load dataset. Once loaded, the dataset can be saved to the project. You can also load datasets under the currently unimplemented Load Components menu item. There you can choose to load only specific things from a layout file.
It's much faster to load datasets from layout file rather than from text because the layout files only encode the analyzed frequencies. That said, datasets are only valid for the alphabet they were intitially analyzed on.
The dataset will not be cleared when you create a new project.

## Swap characters
The analyzer only supports a 3x10 grid of characters at the moment, and therefore, not every character in the alphabet will appear in on the keyboard. To swap the active keys with another in the aphabet, go to edit->swap characters. It will bring up a dialog, and those keys can be click and dragged onto the layout to swap.

## Generation and optimization
Right now generation is slow, but that will be fixed in the future! To generate a layout, go to the analyze tab, then select generate. Currently, the app will freeze and load for a minute or two. Then after its finished, a new layout will appear on screen. Undo will go back to the keyboard state before generation. As of now, generation cannot be canceled. Optimzation is not the same as generation. Optimiaztion will find a local optima up to a certain depth (max 3). Depth 2 should process in under a second, but depth 3 can take minutes.

## Save and Load + Undo Redo
The analyzer supports saving your current configuration to a file. Most user-facing config settings will be placed in the file. When loaded, the app will look exactly as it did when the file was saved. Undo and redo are also implemented allowing easy and precise backtracking.

## Settings
Under edit->settings, you can modify some usability aspects of the application. 

### Keyboard view
The two options are grid and standard. In standard mode, the keyboard will appear with the keys offset as they would be on an actual keyboard, but if you want a more tiled view, you can switch it to grid mode. All the keys will be lined up on the vertical and horizontal axes.

### Heatmap type
The currently displayed heatmap can be selected here. The main type is Char Freq which refers to how often that keys is used.

### Space finger
Trigram statistics are heavily modified by the key used to press space. Many use the same finger almost every time they hit the space key. You can set this to your personal preference, and scores will be adjusted accordingly.
Bug: Right now, changing the space finger does not trigger analysis! So move a key, and switch it back.

### Show finger usage
When enabled, you will see a color code of which fingers are used for which keys. Disabling that will turn the heatmap and border to red for each key.

### Keyboard shape and language
Will all be implemented later, but will just impact default weights and language will impact the alphabet.

## Stats view
At the bottom of the screen, several stats about the keyboard layout are listed. The ones of the left are affected by the spacebar placement and don't give results per finger or hand. Soon you will be able to click on the measurement result to see what their value means.
TODO: Write explanation for each one.

## Coming soon!
1. A way to modify weights from the UI. Right now they are only expressed in the code and are not easily changed unless you know what you're doing.
2. Faster generation. This will soon be done on serveral threads to save time.
3. Editing scissor maps from the UI
4. More heatmaps for things such as the per-key analytics as well as the effort map.

## Heatmaps
There are several types of heatmaps. 

### Char Freq
Char frequency is the simplest. The color gets darker the more the key is used.

### Relative Char Freq
How often your selected key is used compared to other keys. Essentially sets the usage max value to the usage of that key, so if you select a key with low usage, every other key will be completely filled in while if you select a key with high usage, everything will look like Char Freq.

### Pre bigram frequency
Just shows how often each character appears before the selected character.

### Post bigram frequency
Shows how often each character appears after the selected character.

### Bigram Frequency
Shows how often each charater appears with the selected character.

## Nice lesser known features:

### Copy paste
The entire layout can be copied to text by using control-c. A previously copied layout in row/column form can be pasted in with control-v.

### Export to KLC
After your layout is finished, you can export it directly to a klc file which can be loaded by Microsoft Keyboard Layout Creator 1.4
BUG: The qwerty d and g keys appear to be messed up. It'll be fixed soon.
