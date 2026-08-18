## An ACT plugin to track a cleric's remaining Reactive Heals for their group.

The aim of this project is to show remaining Single and Group Reactive count and duration on individual group members.  
This plugin was developed against the Wuoshi TLE server.

### Installation:
Copy the Release DLL into C:\Users\YOUR_USERNAME\AppData\Roaming\Advanced Combat Tracker\Plugins  
Within ACT -> Plugins -> Browse -> Location the DLL -> Add/Enable Plugin  
This will add the ReactiveTracker.dll plugin to the listbox - You may need to locate it in the listbox and click Enable.

### Usage
On the ReactiveTracker Tab select the correct healer type (only Inquisitor and Templar work at the moment).  
Click the checkbox if you have Coercive Healing in your group - this adds an extra tick to both single and group reactives.  
Click the checkbox if you have the Templar EoF 2 set bonus - this adds an extra 4 tick to the Templar single reactive.

Create a macro of  **/whogroup**  and click it whenever the group composition changes.  
I couldn't reliably track the group composition of people leaving / joining or determine who was in an existing group, or when you are dragged to a different raid group.  
Therefore the output of  /whogroup  is used which lists the group members in order. When the group changes, click the macro, and the plugin will know the group composition.

### Todo:
Improve the layout and readability of the configuration tab.  
Have the reactive data move with the player - eg if GroupMember2 leaves then GroupMember3 becomes GroupMember2.  
Detect reactives dropping due to being over-written.  
Add a Transparancy ratio for the overlay window background  
Add an expiration warning to flash or change the background when a proc count or timer threshold is passed.  
Add a proc count and timer threshold for the expiration warning

### Future Work
Track Shaman Wards  
Track Chilling Invigoration when it is released
 
