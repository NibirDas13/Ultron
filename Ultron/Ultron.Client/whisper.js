const Recorder = require('recorder-js');

let audioContext = null;
let recorder = null;
let isRecording = false;

async function startRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        audioContext = new AudioContext();
        recorder = new Recorder(audioContext);
        recorder.init(stream);
        await recorder.start();
        isRecording = true;
        return true;
    } catch(e) {
        console.error('Mic error:', e);
        return false;
    }
}

async function stopRecordingAndTranscribe(groqApiKey) {
    if (!recorder || !isRecording) return null;

    const { blob } = await recorder.stop();
    isRecording = false;

    const formData = new FormData();
    formData.append('file', blob, 'audio.wav');
    formData.append('model', 'whisper-large-v3');

    const response = await fetch('https://api.groq.com/openai/v1/audio/transcriptions', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${groqApiKey}`
        },
        body: formData
    });

    if (!response.ok) {
        console.error('Whisper error:', await response.text());
        return null;
    }

    const data = await response.json();
    return data.text;
}

module.exports = { startRecording, stopRecordingAndTranscribe };