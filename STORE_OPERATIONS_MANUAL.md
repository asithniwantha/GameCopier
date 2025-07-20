# GameDeploy Kiosk - Store Operations Manual

## Quick Start Guide for Retail Employees

### ?? **Goal**: Copy games to customer drives quickly and efficiently

---

## **Step 1: Connect Customer Drives**
- Insert customer USB drives into available USB ports
- Wait 2-3 seconds for drives to appear in the "TARGET DRIVES" section
- The system automatically detects drives every 5 seconds
- Click "Manual Refresh" if drives don't appear immediately

## **Step 2: Find Games**
- Use the search box at the top-left to find games
- Type part of the game name (e.g., "cyber" for Cyberpunk 2077)
- Results appear instantly as you type
- Click "Refresh" next to GAME LIBRARY if new games were added

## **Step 3: Select Games**
- ? Check the boxes next to games the customer purchased
- Game sizes are shown below each name
- Select multiple games if needed

## **Step 4: Select Target Drives**
- ? Check the boxes next to customer drives
- Drive capacity and free space are shown with progress bars
- You can copy to multiple drives at once
- Both removable and fixed drives will appear for testing

## **Step 5: Start Deployment**
- Click the large **"START DEPLOYMENT"** button
- The system checks if there's enough space automatically
- If there's not enough space, a warning message will appear
- Status messages include helpful emoji indicators

## **Step 6: Monitor Progress**
- Individual job progress shows in the right panel
- Overall progress bar shows total completion
- ? Green checkmarks indicate completed jobs
- ? Red X marks indicate failed jobs (rare)

---

## **New Features Added:**

### **?? Automatic Drive Detection**
- Drives are automatically detected every 5 seconds
- No need to manually refresh unless needed
- Drive selections are preserved during automatic refreshes

### **?? Working Settings Button**
- Click the settings gear icon in the top-right
- Refreshes both game library and drives
- Useful for troubleshooting

### **?? Enhanced Status Messages**
- Status messages now include emoji indicators
- More descriptive feedback (e.g., "?? Please select at least one game")
- Real-time updates showing what's selected

### **?? Improved Drive Detection**
- Better detection of various USB drive types
- Includes backup detection for testing (shows fixed drives too)
- More robust error handling

---

## **What if something goes wrong?**

### **Drive not detected:**
- **Wait 5 seconds** - automatic detection is running
- Try unplugging and reconnecting the drive
- Click "Manual Refresh" button under Target Drives
- Click the settings button (??) to force a full refresh
- Make sure drive is not write-protected

### **"Not enough space" error:**
- Check drive capacity vs. game sizes
- Customer may need a larger drive
- Try copying fewer games

### **Copy job fails:**
- Other jobs will continue running
- Check if drive was disconnected
- Failed jobs are clearly marked - try again

### **App seems frozen:**
- The app should never freeze - all operations run in background
- If UI stops responding, close and restart the app
- In-progress copies will need to be restarted

---

## **?? Pro Tips for Speed:**

1. **Use the search** - Don't scroll through hundreds of games
2. **Multiple drives** - Copy to several customer drives at once
3. **USB 3.0** - Encourage customers to use faster drives
4. **Batch processing** - Select all games at once, not one by one
5. **Monitor space** - Check drive capacity before starting
6. **Auto-detection** - Let the system detect drives automatically

---

## **Status Messages Guide:**

| Message | Meaning |
|---------|---------|
| ?? "X games available, Y drives detected" | Normal operation, ready to select |
| ?? "X games selected. Please select drives" | Games chosen, need drives |
| ?? "X drives selected. Please select games" | Drives chosen, need games |
| ? "Ready to deploy!" | Both games and drives selected |
| ?? "Starting deployment..." | Copy jobs are starting |
| "Copying... 45%" | Individual job progress |
| ? "Completed" | Job finished successfully |
| ? "Failed: [reason]" | Job failed (see reason) |
| ? "Deployment completed successfully!" | All jobs finished |

---

## **Emergency Situations:**

### **Customer needs to leave urgently:**
- Click **"CANCEL"** to stop all copying
- Partially copied games will need to be restarted
- Customer can return later to complete

### **Drive runs out of space mid-copy:**
- That specific job will fail and stop
- Other drives will continue copying
- Clear some space on the drive and restart

### **Power outage:**
- All progress is lost
- Restart the application when power returns
- Begin deployment process again

---

## **Daily Startup Checklist:**

- [ ] Launch GameDeploy Kiosk application
- [ ] Verify game library loaded (check game count in status)
- [ ] Test USB port functionality with a known drive
- [ ] Verify automatic drive detection is working (5-second intervals)
- [ ] Ensure adequate free disk space on library drive

---

## **Troubleshooting the Fixes:**

### **If automatic detection isn't working:**
- Click "Manual Refresh" under TARGET DRIVES
- Click the settings button (??) in the header
- Check if drives appear in Windows File Explorer
- Try different USB ports

### **If settings button doesn't work:**
- The button should show "Settings: Refreshing..." message
- It will automatically refresh games and drives
- If nothing happens, restart the application

---

**Remember**: The app now has automatic drive detection and better error handling. Most operations are automatic, and errors are clearly displayed with emoji indicators. When in doubt, use the settings button or restart the deployment!

**Questions?** Check the main README.md file or contact IT support.

---
*GameDeploy Kiosk v1.1 - Now with automatic drive detection and improved user feedback*