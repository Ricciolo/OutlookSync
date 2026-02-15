/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.{razor,html,cshtml}',
    './Pages/**/*.{razor,html,cshtml}',
    './Views/**/*.{razor,html,cshtml}',
    './Shared/**/*.{razor,html,cshtml}',
    './wwwroot/index.html',
  ],
  theme: {
    extend: {
      colors: {
        'outlook-blue': '#0078d4',
      },
    },
  },
  plugins: [],
}
