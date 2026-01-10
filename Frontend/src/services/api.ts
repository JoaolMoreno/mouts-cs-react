import axios from 'axios';
import Cookies from 'js-cookie';

const api = axios.create({
    baseURL: 'http://localhost:5198/api', // Adjust if backend runs on different port
});

api.interceptors.request.use((config) => {
    // Token is HTTP Only, so we technically don't need to inject it manually if we use cookies 
    // AND the backend expects cookies (which it does, based on README).
    // However, axios needs withCredentials: true
    config.withCredentials = true;
    return config;
});

api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            // Handle unauthorized (optional: redirect to login if not already there)
            if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
                window.location.href = '/login';
            }
        }
        return Promise.reject(error);
    }
);

export default api;
