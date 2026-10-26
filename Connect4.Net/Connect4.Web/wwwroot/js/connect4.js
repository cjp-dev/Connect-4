// Connect 4's browser helpers (sounds); the shared ones are in GameShell's gameshell.js.
window.connect4 = (() => {
    const sounds = new Map();
    let queue = Promise.resolve();
    let audio = null;

    return {
        beep() {
            audio ??= new AudioContext();
            const oscillator = audio.createOscillator();
            const gain = audio.createGain();
            oscillator.frequency.value = 880;
            gain.gain.value = 0.1;
            oscillator.connect(gain).connect(audio.destination);
            oscillator.start();
            oscillator.stop(audio.currentTime + 0.1);
        },

        registerSound(name, bytes) {
            sounds.set(name, URL.createObjectURL(new Blob([bytes], { type: 'audio/wav' })));
        },

        // One sound after the other, so a drop is not cut off by the win sound.
        playSound(name) {
            const url = sounds.get(name);
            if (!url) {
                return;
            }
            queue = queue.then(() => new Promise(resolve => {
                const sound = new Audio(url);
                sound.addEventListener('ended', resolve);
                sound.addEventListener('error', resolve);
                sound.play().catch(resolve);
            }));
        },
    };
})();
