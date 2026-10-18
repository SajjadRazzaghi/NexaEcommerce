import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Routes } from '@generouted/react-router';
import './styles/fonts.css';
import './styles/globals.css';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Routes />
    </StrictMode>,
);