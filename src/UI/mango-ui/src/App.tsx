import { BrowserRouter } from 'react-router-dom';
import { AppProviders } from '@/providers/AppProviders';
import { AppRouter } from '@/router/AppRouter';
import { Navbar } from '@/components/Navbar';
import { ChatPopup } from '@/components/ChatPopup';
import './index.css';
import './App.css';

function App() {
  return (
    <AppProviders>
      <BrowserRouter>
        <div className="app">
          <Navbar />
          <main className="main-content">
            <AppRouter />
          </main>
          <ChatPopup />
        </div>
      </BrowserRouter>
    </AppProviders>
  );
}

export default App;
